using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Renci.SshNet;

namespace akash_dep
{
    public class ClientSSH
    {
        public String SSH_IP = "127.0.0.1";
        public int SSH_PORT = 22;
        public String SSH_LOGIN;
        public String SSH_PASS;

        static SshClient m_client;
        static String m_pushed = "";

        public void Connect()
        {
            m_client = new SshClient(SSH_IP, SSH_PORT, SSH_LOGIN, SSH_PASS);
            m_client.Connect();
        }

        public void LoadCfg(JToken cfg)
        {
            SSH_IP = cfg["SSH_IP"].ToString();
            SSH_PORT = cfg["SSH_PORT"].ToObject<int>();
            SSH_LOGIN = cfg["SSH_LOGIN"].ToString();
            SSH_PASS = cfg["SSH_PASS"].ToString();
        }

        public void Push(String cmd)
        {
            if (m_pushed.Length > 0)
            {
                m_pushed += ";\n";
            }

            m_pushed += cmd;
        }

        public void Clear()
        {
            m_pushed = "";
        }

        public void ShowErrorInfo(SshCommand res)
        {
            String err = res.Error;
            if (String.IsNullOrEmpty(err)) return;

            if (res.ExitStatus == 0)
            {
                if (!err.Contains("gas estimate:"))
                {
                    Console.WriteLine("info->\n" + err);
                }
            }
            else
            {
                Console.WriteLine("err->\n" + err);
            }
        }

        public String Send()
        {
            m_pushed += "\n";
            var cmdRes = m_client.CreateCommand(m_pushed);
            var res = cmdRes.Execute();
            String err = cmdRes.Error;

            ShowErrorInfo(cmdRes);

            if (!String.IsNullOrEmpty(err))
            {
                int tries = 0;

                while (err.Contains("post failed: Post") && err.Contains(": EOF") && tries < 2)
                {
                    tries++;
                    cmdRes = m_client.CreateCommand(m_pushed);
                    res = cmdRes.Execute();
                    err = cmdRes.Error;
                    ShowErrorInfo(cmdRes);
                }
            }

            File.WriteAllText("akash.txt", m_pushed);

            Clear();
            return res;
        }
    }
}
