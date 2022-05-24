using YamlDotNet.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Globalization;

namespace akash_dep
{

    public static class Converters
    {

        public static double AKT_PRICE;//UAKT->AKT->USD per month conversion
        //info https://docs.google.com/spreadsheets/d/1q8ExwZBvbhqlHVP1fGOZg69kyxWG5iuip3m-KVONwP8/edit#gid=0

        public static void LoadCfg(JToken cfg)
        {
            AKT_PRICE = cfg["AKT_PRICE"].ToObject<double>();
        }

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


        public static String DoubleToStr2Dig(double val)
        {
            return val.ToString("F", CultureInfo.InvariantCulture);
        }

        public static double AKTtoUSD(double akt)
        {
            return akt * AKT_PRICE;
        }

        public static double UAKTtoAKTMonthly(double uakt)
        {
            double akt = UAKTtoAKT(uakt);
            var now = DateTime.Now;
            int numDays = DateTime.DaysInMonth(now.Year, now.Month);
            return (86400 * numDays * (akt) / 5.976);
        }

        public static double UAKTtoUSDMonthly(double uakt)
        {
            var uaktMon = UAKTtoAKTMonthly(uakt);
            return uaktMon * AKT_PRICE;
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
                if(js.Contains("denom"))
                {
                    String type = js["denom"].ToString();
                    if (type != "uakt")
                    {
                        Console.WriteLine("invalid denom " + type + " in " + js.ToString());
                        return 0;
                    }
                }
                var amt = js["amount"].ToObject<double>();
                return amt;
            }
            catch
            {
                Console.WriteLine("invalid json for balance conv " + js.ToString());
                return 0.0;
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