using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;

namespace akash_dep
{
    public class InstanceData
    {
        public JToken js;
        public Instance inst;
    }

    public class InstanceList
    {

        Wallet m_wallet;
        List<InstanceData> m_instances = new List<InstanceData>();

        public InstanceList(ref Wallet wallet)
        {
            m_wallet = wallet;
        }

        public void UpdateExec(ref String exec)
        {
            exec = Akash.Prepare(exec);
            exec = m_wallet.Prepare(exec);
        }


        const int kMaxRetry = 3;

        public delegate bool RetryFunType();
        bool RetryFun(RetryFunType fun)
        {
            for (int i = 0; i < kMaxRetry; i++)
            {
                bool ok = fun();
                if (ok) return true;
            }
            return false;
        }

        public bool Query()
        {
            Console.WriteLine("getting deployment list");

            String exec;
            exec = "query deployment list --node $AKASH_NODE --owner $AKASH_ACCOUNT_ADDRESS " +
                "--chain-id $AKASH_CHAIN_ID --output json --state active --count-total --limit 500";
                
            UpdateExec(ref exec);
            Akash.PushAkash(exec);

            var res = Akash.Send();
            File.WriteAllText("deployments.txt", res);

            JToken js = Converters.STRtoJS(res);
            if (js == null)
            {
                Console.WriteLine("invalid js");
                return false;
            }


            var deployments = (JArray)js["deployments"];
            long numSubDeployments = 0;
            long numDeployments = 0;
            double totalBalance = 0;

            m_instances.Clear();

            //var progress = new ProgressConsole(deployments.Count,"query deployments");
            // parse and fill data
            foreach (var dep in deployments)
            {
                //progress.Increment();

                JToken depjs = dep["deployment"];
                Instance inst = new Instance(ref m_wallet);
                InstanceData data = new InstanceData();
                data.inst = inst;
                data.js = dep;

                long numSubDeps = GetNumInstFromDepJS(dep);

                m_instances.Add(data);
                if(!inst.LoadFromDepJson(depjs)) continue;

                JToken escrow = dep["escrow_account"];
                JToken balance = escrow["balance"];
                var balance_akt = Converters.UAKTJStoAKT(balance);
                totalBalance += balance_akt;

                numSubDeployments += numSubDeps;
                numDeployments++;
            }

            Console.WriteLine("total deployments " + numDeployments);
            Console.WriteLine("total subMachines " + numSubDeployments);
            double totalBalanceUSD = Converters.AKTtoUSD(totalBalance);
            Console.WriteLine("total locked balance: " + Converters.DoubleToStr2Dig(totalBalanceUSD) +"$");
            return true;
        }

        public long GetNumInstFromDepJS(JToken dep)
        {
            JToken group = dep["groups"][0];
            JToken resources = group["group_spec"]["resources"][0];
            long numSubDeps = resources["count"].ToObject<long>();
            return numSubDeps;
        }

        public void Stats()
        {
            Dictionary<long, long> MachineStats = new Dictionary<long, long>();

            //var progress = new ProgressConsole(m_instances.Count, "Stats");
            foreach (var data in m_instances)
            {
                //progress.Increment();

                var dep = data.js;
                JToken depjs = dep["deployment"];
                String state = depjs["state"].ToString();

                JToken escrow = dep["escrow_account"];
                String money_state = escrow["state"].ToString();

                JToken balance = escrow["balance"];
                JToken transferred = escrow["transferred"];

                var balance_akt = Converters.UAKTJStoAKT(balance);
                var transfer_akt = Converters.UAKTJStoAKT(transferred);

                long numSubDeps = GetNumInstFromDepJS(dep);


                

                Instance inst = data.inst;
                Console.WriteLine(inst.m_dseq + "->" + state + " / " + 
                money_state + " balance " + Converters.DoubleToStr2Dig(Converters.AKTtoUSD(balance_akt)) + 
                    "$ transfered " + Converters.DoubleToStr2Dig(Converters.AKTtoUSD(transfer_akt)) +
                    "$ cores "+ numSubDeps);

                long value = 0;
                if (MachineStats.TryGetValue(numSubDeps, out value))
                {
                    MachineStats[numSubDeps]++;
                }
                else
                {
                    MachineStats.Add(numSubDeps, 1);
                }

            }

            foreach(var inst in MachineStats)
            {
                Console.WriteLine("CPUs: " + inst.Key + " count " + inst.Value);
            }
        }

        public bool Close(bool closeNoLease,bool closeClosedLease,bool closeExpensive)
        {
            var progress = new ProgressConsole(m_instances.Count, "Close");

            double totalBidsBalance = 0;

            int numClosed = 0;
            foreach (var data in m_instances)
            {
                progress.Increment();
                var dep = data.js;
                JToken depjs = dep["deployment"];
                String state = depjs["state"].ToString();

                JToken escrow = dep["escrow_account"];
                String money_state = escrow["state"].ToString();

                //if (state == "active") continue;
                //if (money_state == "open") continue; // Ignore those closed because of the lack of funds
                Instance inst = data.inst;

                bool need2close = false;

                if(!inst.CheckLease())
                {
                    int num = inst.GetNumLeases();
                    Console.WriteLine(inst.m_dseq + " num leases " + num);
                    if (num==0 && closeNoLease)
                    {
                        need2close = true;
                    }
                }


                String lease_state = inst.GetLeaseState();
                JToken lease_price = inst.GetLeasePrice();

                double curPrice = 0;
                if(lease_price!=null)
                {
                    curPrice += Converters.UAKTJSget(lease_price);
                }
                else
                {
                    Console.WriteLine("err no bid price!");
                }

                double perCoreUSD = Converters.UAKTtoUSDMonthly(curPrice);

                long numSubDeps = GetNumInstFromDepJS(dep);
                if (perCoreUSD>0.0)
                {
                    totalBidsBalance += perCoreUSD * numSubDeps;
                }

                if(closeExpensive && perCoreUSD>Instance.AKASH_PRICE_LIMIT_CORE)
                {
                    need2close = true;
                }


                Console.WriteLine("cores " + numSubDeps + " " + inst.m_dseq + " state: " + state + " money: " + money_state + 
                " lease: " + lease_state + " price " + Converters.DoubleToStr2Dig(perCoreUSD) + "$/core");

                if(lease_state=="closed" && closeClosedLease)
                {
                    need2close = true;
                }

                if (!need2close)
                {
                    continue;
                }


                if (!RetryFun(() => inst.Close())) continue;

                numClosed++;
            }
            Console.WriteLine("closing dead finished closed: "+numClosed);
            Console.WriteLine("total active monthly balance " + Converters.DoubleToStr2Dig(totalBidsBalance) + "$");
            return true;
        }

        public void DoDeposits(long refillAmount)
        {
            var progress = new ProgressConsole(m_instances.Count, "DoDeposits");
            foreach (var data in m_instances)
            {
                progress.Increment();

                var dep = data.js;
                JToken depjs = dep["deployment"];
                String state = depjs["state"].ToString();

                JToken escrow = dep["escrow_account"];
                String money_state = escrow["state"].ToString();

                if (state == "closed") continue;

                JToken balance = escrow["balance"];
                var balanceAkt = Converters.UAKTJStoAKT(balance);
                if (balanceAkt >= refillAmount || balanceAkt<0) continue; // Skip filled or invalid

                Instance inst = data.inst;
                double depAmount = refillAmount - balanceAkt; // Refill 5akt

                RetryFun(() => inst.Deposit(Converters.AKTtoUAKT(depAmount)));
            }
        }



        public void UpdateManifests()
        {
            var progress = new ProgressConsole(m_instances.Count, "UpdateManifests");

            foreach (var data in m_instances)
            {
                progress.Increment();

                var dep = data.js;
                JToken depjs = dep["deployment"];
                String state = depjs["state"].ToString();

                JToken escrow = dep["escrow_account"];
                String money_state = escrow["state"].ToString();

                if (state == "closed") continue;
                Instance inst = data.inst;
                if (!inst.CheckLease()) continue; // Must load all lease data

                long numInstances = GetNumInstFromDepJS(dep);// Correct filling require correct manifest

                inst.PrepareYml(numInstances); // Prepare new manifest

                String lease_state = inst.GetLeaseState();
                if (lease_state == "closed") continue; //Ignore closed leases

                if (!RetryFun(() => inst.SendManifestEvent())) continue;
                if (!RetryFun(() => inst.SendManifest())) continue;

            }
        }
    }
}