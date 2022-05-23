using YamlDotNet.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;

namespace akash_dep
{

    public static class Converters
    {
        public static String YAMLtoJSON(String yml)
        {
            try
            {
                var r = new StringReader(yml);
                var des = new Deserializer();
                var yaml = des.Deserialize(r);

                JsonSerializer ser = new JsonSerializer();
                var w = new StringWriter();
                ser.Serialize(w, yaml);
                return w.ToString();
            }
            catch
            {
                return null;
            }
        }
        public static JToken YAMLtoJSONToken(String yml)
        {
            String js_raw = YAMLtoJSON(yml);
            if (js_raw == null) return null;
            return STRtoJS(js_raw);
        }
        public static JToken STRtoJS(String js)
        {
            try
            {
                return JToken.Parse(js);
            }
            catch
            {
                return null;
            }
        }
        public static double UAKTtoAKT(double uakt)
        {
            return uakt / Math.Pow(10, 6);
        }
        public static double AKTtoUAKT(double akt)
        {
            return Math.Round(akt * Math.Pow(10, 6));
        }
        public static double UAKTJSget(JToken js)
        {
            try
            {
                String type = js["denom"].ToString();
                if (type != "uakt")
                {
                    Console.WriteLine("invalid denom " + type + " in " + js.ToString());
                    return 0;
                }
                var denom = js["amount"].ToObject<double>();
                return denom;
            }
            catch
            {
                Console.WriteLine("invalid json for balance conv " + js.ToString());
                return 0;
            }
        }
        public static double UAKTJStoAKT(JToken js)
        {
            try
            {
                var amt = UAKTJSget(js);
                return UAKTtoAKT(amt);
            }
            catch
            {
                Console.WriteLine("invalid json for balance uconv " + js.ToString());
                return 0;
            }
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

    }

}