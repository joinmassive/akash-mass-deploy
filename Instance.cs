

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace akash_dep
{

    public class Instance
    {
        readonly Wallet m_wallet;
        public static String AKASH_YML_PATH = "deploy.yml";
        public static String AKASH_YML_EDITED_PATH = "deploy_submit.yml";
        public static String AKASH_HOME = "~/.akash";
        public static String GAS_CONFIG = "--gas-prices=\"0.025uakt\" --gas=\"auto\" --gas-adjustment=1.25";
        public static long AKASH_PRICE_LIMIT = 180;// no more then this, otherwise it will be too expensive

        public long m_dseq = -1;
        public String m_provider = "";

        public EnvVarsReplacer m_replacer = new EnvVarsReplacer();

        public JArray m_currentLeases = null;

        static Dictionary<String, String> badList = new Dictionary<string, string>();
        public void MarkBad()
        {
            if(CheckIsBad(m_provider))
            {
                Console.WriteLine("duplicate bad");
                return;
            }
            String badID = m_provider;
            if (String.IsNullOrEmpty(badID)) return;
            var stream = File.AppendText("bad.txt");
            stream.WriteLine(badID);
            stream.Close();

            badList.Add(badID, "2");
            Console.WriteLine("added to ban " + badID);
        }

       
        public static void LoadBad()
        {
            try
            {
                var stream = File.ReadAllLines("bad.txt");
                foreach (var st in stream)
                {
                    if (String.IsNullOrWhiteSpace(st)) continue;
                    badList.Add(st, "1");
                    Console.WriteLine("added " + st + " to ban");
                }
            }
            catch
            {

            }
        }

        public bool CheckIsBad(String prov)
        {
            String value;
            if(badList.TryGetValue(prov,out value))
            {
                Console.WriteLine("banned " + prov);
                return true;
            }
            return false;
        }

        public Instance(ref Wallet wallet)
        {
            m_wallet = wallet;

            m_replacer.Add("AKASH_HOME", AKASH_HOME);
        }

        public static void LoadCfg(JToken cfg)
        {
            AKASH_YML_PATH = cfg["AKASH_YML_PATH"].ToString();
            AKASH_YML_EDITED_PATH = cfg["AKASH_YML_EDITED_PATH"].ToString();
            AKASH_HOME = cfg["AKASH_HOME"].ToString();
            GAS_CONFIG = cfg["GAS_CONFIG"].ToString();
            AKASH_PRICE_LIMIT = cfg["AKASH_PRICE_LIMIT"].ToObject<long>();
        }

        // Currently basic renaming to fix problems in naming in pool statistics
        public void PrepareYml(long numInstances)
        {
            String text = File.ReadAllText(AKASH_YML_PATH);
            String text2replace = "aktDEP_RandomName_10";
            String rndDeployText = "aktDEP_"+Converters.RandomString(5); // Current limit of the pool we are using
            rndDeployText += "_" + numInstances;//add num instances
            text = text.Replace(text2replace, rndDeployText);

            // update correct number of instances
            String inst2replace = "count: 10";
            text = text.Replace(inst2replace, "count: "+numInstances);

            Console.WriteLine("new name " + rndDeployText);
            File.WriteAllText(AKASH_YML_EDITED_PATH,text);
        }

        public void ClearCaches()
        {
            File.Delete("bids.txt");
            File.Delete("check.txt");
            File.Delete("closed.txt");
            File.Delete("deploy.txt");
            File.Delete("lease.txt");
            File.Delete("manifest.txt");
        }

        public bool LoadFromDepJson(JToken deployment)
        {
            try
            {
                var deployment_id = deployment["deployment_id"];
                m_dseq = deployment_id["dseq"].ToObject<long>();
                SetSeqVars();// Update the db for future uses
            }
            catch
            {
                Console.WriteLine("loading vars from js failed " + deployment.ToString());
                return false;
            }
            return true;
        }


        public static long FindDseq(JToken js)
        {
            JArray logs = (JArray)js["logs"];

            JArray events = (JArray)logs[0]["events"];
            foreach (var ev in events)
            {
                if (ev["type"].ToString() == "akash.v1")
                {
                    var attributes = (JArray)ev["attributes"];
                    foreach (var at in attributes)
                    {
                        if (at["key"].ToString() == "dseq")
                        {
                            return at["value"].ToObject<long>();
                        }
                    }
                }

            }
            return -1;
        }

        public void SetSeqVars()
        {
            m_replacer.Add("AKASH_GSEQ", "1");
            m_replacer.Add("AKASH_OSEQ", "1");
            m_replacer.Add("AKASH_DSEQ", m_dseq.ToString());
        }

        public void SetProviderVars()
        {
            m_replacer.Add("AKASH_PROVIDER", m_provider);
        }

        public String Prepare(String val)
        {
            return m_replacer.Prepare(val);
        }

        public void UpdateExec(ref String exec)
        {
            exec = Akash.Prepare(exec);
            exec = m_wallet.Prepare(exec);
            exec = Prepare(exec);
        }

        public bool Create(long numInstances)
        {
            PrepareYml(numInstances);
            Console.WriteLine("creating new deployment, inst "+numInstances);

            String exec = "tx deployment create \"" + AKASH_YML_EDITED_PATH + "\" " +
                "--from $AKASH_KEY_NAME --node $AKASH_NODE --chain-id $AKASH_CHAIN_ID --keyring-backend $AKASH_KEYRING_BACKEND " +
                "--yes " + PrepareGas();
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            String js_str;
            if (File.Exists("deploy.txt"))
            {
                Console.WriteLine("cached deploy.txt");
                Akash.Clear();
                js_str = File.ReadAllText("deploy.txt");
            }
            else
            {
                js_str = Akash.Send();

            }


            JToken js = Converters.STRtoJS(js_str);

            try
            {
                m_dseq = FindDseq(js);
            }
            catch
            {
                Console.WriteLine("invalid dseq");
                return false;
            }


            if (m_dseq == -1)
            {
                return false;
            }

            File.WriteAllText("deploy.txt", js_str); // Save valid
            Console.WriteLine("creating new deployment OK, got dseq " + m_dseq);
            return true;
        }

        public String FindBestLease(JToken js)
        {
            Console.WriteLine(m_dseq + " searching for best lease");

            JToken curBest = null;

            int numOpened = 0;
            JArray bids = (JArray)js["bids"];

            Console.WriteLine("got bids: " + bids.Count);
            foreach (var bid_ in bids)
            {
                var bid = bid_["bid"];
                String state = bid["state"].ToString();
                if (state != "open") continue;
                numOpened++;

                String priceType = bid["price"]["denom"].ToString();
                if (priceType != "uakt")
                {
                    continue;// ignore all new denom specs
                }

                JToken bidPriceJS = bid["price"];
                //long curPrice = bidPriceJS["amount"].ToObject<double>();
                double curPrice = Converters.UAKTJSget(bidPriceJS);


                if (curPrice > m_wallet.GetNumAKT() || curPrice > AKASH_PRICE_LIMIT)
                {
                    Console.WriteLine("too expensive " + curPrice + "uakt");
                    continue;// our of money or too expensive for us
                }
                else
                {
                    Console.WriteLine("got bid " + curPrice + "uakt");
                }

                String curLeaseID = bid["bid_id"]["provider"].ToString();
                if (CheckIsBad(curLeaseID)) continue; // Skipped banned ones


                if (curBest == null) // Save primary
                {
                    curBest = bid;
                    continue;
                }

                //long bestPrice = curBest["price"]["amount"].ToObject<long>();
                JToken bestPriceJS = curBest["price"];
                double bestPrice = Converters.UAKTJSget(bestPriceJS);

                if (curPrice < bestPrice)
                {
                    curBest = bid;
                    continue;
                }
            }
            if (curBest == null)
            {
                Console.WriteLine("no good lease was found! numOpened: " + numOpened);
                return null;
            }

            String leaseID = curBest["bid_id"]["provider"].ToString();

            JToken priceJS = curBest["price"];
            double price = Converters.UAKTJSget(priceJS);

            Console.WriteLine("good lease price " + price + "aukt");
            Console.WriteLine("good lease was found id: " + leaseID);
            return leaseID;
        }

        public bool CreateLease()
        {
            Console.WriteLine(m_dseq + " creating lease");

            SetSeqVars();
            String exec = "query market bid list --owner=$AKASH_ACCOUNT_ADDRESS --node $AKASH_NODE --dseq $AKASH_DSEQ";
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            String js_raw;
            if (File.Exists("bids.txt"))
            {
                Console.WriteLine("cached bids.txt");
                Akash.Clear();
                js_raw = File.ReadAllText("bids.txt");
            }
            else
            {
                String yam_str = Akash.Send();
                js_raw = Converters.YAMLtoJSON(yam_str);
            }


            JToken js = Converters.STRtoJS(js_raw);
            //File.WriteAllText("/home/kifa/akash/akash/bin/bids_js.txt", js_raw);

            try
            {
                JArray bids = (JArray)js["bids"];
                if (bids.Count == 0)
                {
                    Console.WriteLine("bids are not ready");
                    return false;
                }
                m_provider = FindBestLease(js);
            }
            catch
            {
                Console.WriteLine("bids have bad data");
                return false;
            }

            if (m_provider != null)
            {
                Console.WriteLine("lease was queried");
                File.WriteAllText("bids.txt", js_raw);
                return true;
            }
            return false;
        }
        public bool SelectLease()
        {
            Console.WriteLine(m_dseq + " selecting lease "+m_provider);
            if (File.Exists("lease.txt"))
            {
                Console.WriteLine("cached lease.txt");
                return true;
            }

            SetSeqVars();
            SetProviderVars();
            String exec = "tx market lease create --chain-id $AKASH_CHAIN_ID --node $AKASH_NODE " +
                "--owner $AKASH_ACCOUNT_ADDRESS --dseq $AKASH_DSEQ --gseq $AKASH_GSEQ --oseq $AKASH_OSEQ --keyring-backend $AKASH_KEYRING_BACKEND " +
                "--provider $AKASH_PROVIDER --from $AKASH_KEY_NAME --yes " + PrepareGas();
            UpdateExec(ref exec);

            Akash.PushAkash(exec);
            String yam_str = Akash.Send();
            if (!String.IsNullOrEmpty(yam_str))
            {
                Console.WriteLine("selecting lease ok");
                File.WriteAllText("lease.txt", yam_str);
                return true;
            }
            else
            {
                Console.WriteLine("selecting lease failed");
                return false;
            }
        }

        //active, closed
        public String GetLeaseState()
        {
            // get lease info[currently fetch primary]
            if (m_currentLeases==null || m_currentLeases.Count==0)
            {
                Console.WriteLine("invoking getLeaseState without checkLeast, no info provided");
                return "dead";
            }

            // get lease info[currently fetch primary]

            try
            {
                JToken lease = m_currentLeases[0]["lease"];
                JToken leaseID = lease["lease_id"];
                String lease_state = lease["state"].ToString();
                return lease_state;
            }
            catch
            {
                return "none";
            }
        }

        public JToken GetLeasePrice()
        {
            if (m_currentLeases == null || m_currentLeases.Count == 0)
            {
                return null;
            }

            try
            {
                JToken lease = m_currentLeases[0]["lease"];
                JToken price = lease["price"];
                return price;
            }
            catch
            {
                return "none";
            }

        }

        public int GetNumLeases()
        {
            if(m_currentLeases==null)
            {
                return -1;
            }
            return m_currentLeases.Count;
        }

        public bool CheckLease()
        {
            Console.WriteLine(m_dseq+" checking lease");
            SetSeqVars();
            SetProviderVars();

            String exec = "query market lease list --owner $AKASH_ACCOUNT_ADDRESS --node $AKASH_NODE --dseq $AKASH_DSEQ";
            UpdateExec(ref exec);

            Akash.PushAkash(exec);
            String yam_str = Akash.Send();
            JToken js = Converters.YAMLtoJSONToken(yam_str);

            try
            {
                m_currentLeases = (JArray)js["leases"];
            }
            catch
            {
                Console.WriteLine("got invalid json");
                return false;
            }


            if (m_currentLeases.Count > 0)
            {
                //Console.WriteLine("checking lease OK");
                File.WriteAllText("check.txt", js.ToString());

                // We also load new values from lease for future uses or control
                JToken lease = m_currentLeases[0]["lease"];
                JToken leaseID = lease["lease_id"];
                m_provider = leaseID["provider"].ToString();
                return true;
            }
            else
            {
                Console.WriteLine("0 leases");
                File.Delete("lease.txt");
            }
            return false;
        }

        public bool SendManifestEvent()
        {
            Console.WriteLine(m_dseq + " sending manifest event");

            SetSeqVars();
            SetProviderVars();
            String exec = "tx deployment update \"" + AKASH_YML_EDITED_PATH + "\" " +
            "--dseq $AKASH_DSEQ --from $AKASH_KEY_NAME --chain-id $AKASH_CHAIN_ID " +
            "--node $AKASH_NODE --yes "+ PrepareGas();
            UpdateExec(ref exec);
            Akash.PushAkash(exec);
            String js_str = Akash.Send();
            JToken js = Converters.STRtoJS(js_str);

            try
            {
                String events = js["logs"][0]["events"].ToString();
                if (events.Contains("deployment-updated") && events.Contains("update-deployment"))
                {
                    Console.WriteLine("sending manifest event ok");
                    File.WriteAllText("manifest_event.txt", js_str);
                    return true;
                }
                else
                {
                    Console.WriteLine("err in manifests: " + js_str);
                }
            }
            catch
            {
                Console.WriteLine("crash in manifests: " + js_str);
            }
            return false;
        }

        public bool SendManifest()
        {
            Console.WriteLine(m_dseq + " sending manifest");

            SetSeqVars();
            SetProviderVars();
            String exec = "provider send-manifest \"" + AKASH_YML_EDITED_PATH + "\" " +
                "--node $AKASH_NODE --dseq $AKASH_DSEQ --provider $AKASH_PROVIDER --keyring-backend $AKASH_KEYRING_BACKEND " +
                "--home $AKASH_HOME --from $AKASH_KEY_NAME";
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            String yam_str = Akash.Send();


            if (yam_str.Contains("status: PASS")) // Fails for converting to json, so we query directly
            {
                Console.WriteLine("sending manifest ok");
                File.WriteAllText("manifest.txt", yam_str);
                return true;
            }
            else
            {
                Console.WriteLine("sending manifest failed");
            }
            return false;
        }

        public bool Close()
        {
            Console.WriteLine(m_dseq + " closing");
            String exec = "tx deployment close --node $AKASH_NODE --chain-id $AKASH_CHAIN_ID " +
                "--dseq $AKASH_DSEQ  --owner $AKASH_ACCOUNT_ADDRESS --from $AKASH_KEY_NAME " +
                "--keyring-backend $AKASH_KEYRING_BACKEND --yes " + PrepareGas();
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            String yam_str = Akash.Send();
            File.WriteAllText("closed.txt", yam_str);

            return true;
        }

        String PrepareGas()
        {
            return GAS_CONFIG;
        }

        public bool Deposit(double uakt)
        {

            Console.WriteLine(m_dseq + " depositing " + Converters.UAKTtoAKT(uakt) + "akt to "+m_dseq);
            String exec = "tx deployment deposit " + uakt + "uakt --node $AKASH_NODE --chain-id $AKASH_CHAIN_ID " +
                "--dseq $AKASH_DSEQ --owner $AKASH_ACCOUNT_ADDRESS --from $AKASH_KEY_NAME " +
                "--keyring-backend $AKASH_KEYRING_BACKEND --yes " + PrepareGas();
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            String yam_str = Akash.Send();

            JToken js = Converters.STRtoJS(yam_str);

            if(yam_str==null || js==null)
            {
                Console.WriteLine("invalid data "+yam_str);
                return false;
            }

            JArray events = (JArray)js["logs"][0]["events"];
            bool checkTransfer = false;
            foreach(var ev in events)
            {
                if(ev["type"].ToString()== "transfer")
                {
                    checkTransfer = true;
                }
            }

            if(!checkTransfer)
            {
                Console.WriteLine("failed transfer");
                return false;
            }

            File.WriteAllText("deposit.txt", yam_str);



            Console.WriteLine("deposit ok");
            return true;
        }
    }

}