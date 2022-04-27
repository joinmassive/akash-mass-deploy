using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Renci.SshNet;

namespace akash_dep
{
    public static class CFG
    {
        public static String AKASH_NODE;

        public static String AKASH_EXE;

        public static String AKASH_KEYRING_BACKEND;
        public static String AKASH_NET;
        public static String AKASH_VERSION;
        public static String AKASH_CHAIN_ID;

        public static EnvVarsReplacer m_replacer = new EnvVarsReplacer();

        public static ClientSSH ssh = new ClientSSH();

        public static void Connect()
        {
            ssh.Connect();
        }

        public static void LoadCfg(JToken cfg)
        {
            ssh.LoadCfg(cfg);

            AKASH_NODE = cfg["AKASH_NODE"].ToString();
            AKASH_EXE = cfg["AKASH_EXE"].ToString();
            AKASH_KEYRING_BACKEND = cfg["AKASH_KEYRING_BACKEND"].ToString();
            AKASH_NET = cfg["AKASH_NET"].ToString();
            AKASH_VERSION = cfg["AKASH_VERSION"].ToString();
            AKASH_CHAIN_ID = cfg["AKASH_CHAIN_ID"].ToString();
        }

        public static void PushAkash(String cmd)
        {
            ssh.Push(CFG.AKASH_EXE + " " + cmd);
        }

        public static void Push(String cmd)
        {
            ssh.Push(cmd);
        }

        public static String Send()
        {
            return ssh.Send();
        }

        public static void Clear()
        {
            ssh.Clear();
        }

        public static String Prepare(String val)
        {
            return m_replacer.Prepare(val);
        }

        public static void EvalVars()
        {
            m_replacer.Add("AKASH_NET", AKASH_NET);
            m_replacer.Evaluate("AKASH_NODE", AKASH_NODE);

            m_replacer.Evaluate("AKASH_VERSION", AKASH_VERSION);
            m_replacer.Evaluate("AKASH_CHAIN_ID", AKASH_CHAIN_ID);

            m_replacer.Add("AKASH_KEYRING_BACKEND", AKASH_KEYRING_BACKEND);

            m_replacer.Print();
        }
    }
}
