using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace akash_dep
{
    public class Wallet
    {
        public static String AKASH_KEY_NAME = "";
        public static String AKASH_ACCOUNT_ADDRESS;
        JToken m_walletJS;
        public long m_amount;
        public EnvVarsReplacer m_replacer = new EnvVarsReplacer();

        public void LoadCfg(JToken cfg)
        {
            AKASH_KEY_NAME = cfg["AKASH_KEY_NAME"].ToString();
            AKASH_ACCOUNT_ADDRESS = cfg["AKASH_ACCOUNT_ADDRESS"].ToString();
        }

        public String Prepare(String val)
        {
            return m_replacer.Prepare(val);
        }

        public void EvalVars()
        {
            m_replacer.Add("AKASH_KEY_NAME", AKASH_KEY_NAME);
            m_replacer.Evaluate("AKASH_ACCOUNT_ADDRESS", AKASH_ACCOUNT_ADDRESS);
            m_replacer.Print();
        }

        public long GetNumAKT()
        {
            return m_amount;
        }

        public void ClearCache()
        {
            File.Delete("wallet.txt");
        }

        public bool Update()
        {
            Console.WriteLine("getting wallet status");

            if (File.Exists("wallet.txt"))
            {
                Console.WriteLine("cached wallet.txt");
                String json = File.ReadAllText("wallet.txt");
                m_walletJS = Converters.STRtoJS(json);
            }
            else
            {
                var exec = "query bank balances --node $AKASH_NODE $AKASH_ACCOUNT_ADDRESS";
                exec = Akash.Prepare(exec);
                exec = Prepare(exec);
                Akash.PushAkash(exec);

                String yml = Akash.Send();

                String json = Converters.YAMLtoJSON(yml);
                m_walletJS = Converters.STRtoJS(json);

                File.WriteAllText("wallet.txt", json);
            }

            try
            {
                JToken balance = m_walletJS["balances"][0];
                m_amount = balance["amount"].ToObject<long>();

                var amount_akt = Converters.UAKTJStoAKT(balance);
                Console.WriteLine("getting wallet status ok, amount " + amount_akt);
            }
            catch
            {
                Console.WriteLine("invalid wallet json");
                return false;
            }

            return true;
        }
    }
}
