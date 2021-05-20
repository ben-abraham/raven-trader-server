using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace raven_trader_server
{
    public class Utils
    {
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static JObject FullExternalTXDecode(string txid, bool testnet = true)
        {
            var rc = new RestClient(testnet ? "https://rvnt.cryptoscope.io/" : "https://rvn.cryptoscope.io/");
            var rr = new RestRequest($"api/getrawtransaction/?txid={txid}&decode=1");
            var resp = rc.Execute(rr);
            return JObject.Parse(resp.Content);
        }
    }
}
