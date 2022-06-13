using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace akash_dep
{
    public class EnvVarsReplacer
    {
        Dictionary<String, String> m_evaluated = new Dictionary<string, string>();

        public String Prepare(String val)
        {
            foreach (var eval in m_evaluated)
            {
                val = val.Replace("$" + eval.Key, eval.Value);
            }

            return val;
        }

        public void Add(String key, String val)
        {
            val = val.Replace("\n", "");
            m_evaluated[key] = val;
        }

        public void Evaluate(String key, String value)
        {
            value = Prepare(value); // replace with known values
            Akash.Push("echo " + value);
            Add(key, Akash.Send());
        }

        public void Print()
        {
            foreach (var eval in m_evaluated)
            {
                Console.WriteLine(eval.Key + "=" + eval.Value);
            }
        }
    }
}
