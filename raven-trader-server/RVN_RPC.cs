using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace raven_trader_server
{
    public class RVN_RPC
    {
        private RestClient RPC_Client = new RestClient();

        public IConfiguration Configuration { get; }
        public ILogger<RVN_RPC> Logger { get; }

        public RVN_RPC(IConfiguration configuration, ILogger<RVN_RPC> logger)
        {
            Configuration = configuration;
            Logger = logger;

            RPC_Client = new RestClient($"http://{configuration["RPC_HOST"]}:{configuration["RPC_PORT"]}");
            RPC_Client.Authenticator = new HttpBasicAuthenticator(configuration["RPC_USER"], configuration["RPC_PASSWORD"]);

            Logger.LogInformation($"RPC Startup: {RPC_Client.BaseUrl}");
        }

        //Specific helper functions
        public JObject GetBlockchainInfo()
        {
            return DoRPC("getblockchaininfo").Result;
        }

        public JObject DecodeRawTransaction(string Hex)
        {
            JObject payload = JObject.FromObject(new { hexstring = Hex });
            return DoRPC("decoderawtransaction", payload).Result;
        }

        public JObject SignRawTransaction(string Hex)
        {
            JObject payload = JObject.FromObject(new { hexstring = Hex });
            return DoRPC("signrawtransaction", payload).Result;
        }

        internal string GetBlockHash(int Number)
        {
            JObject payload = JObject.FromObject(new { height = Number });
            return DoRPC<string>("getblockhash", payload).Result;
        }

        internal JObject GetBlock(string Hash, int Verbosity = 1)
        {
            JObject payload = JObject.FromObject(new { blockhash = Hash, verbosity = Verbosity});
            return DoRPC("getblock", payload).Result;
        }

        internal JObject GetRawTransaction(string TXID, bool Verbose = true)
        {
            JObject payload = JObject.FromObject(new { txid = TXID, verbose = Verbose});
            return DoRPC("getrawtransaction", payload).Result;
        }

        internal JObject GetTXOut(string TXID, int Vout)
        {
            JObject payload = JObject.FromObject(new { txid = TXID, n = Vout});
            return DoRPC("gettxout", payload).Result;
        }

        internal JObject GetAssetData(string AssetName)
        {
            JObject payload = JObject.FromObject(new { asset_name = AssetName });
            return DoRPC("getassetdata", payload).Result;
        }

        internal List<string> ListAssets(string SearchString)
        {
            JObject payload = JObject.FromObject(new { asset = SearchString });
            return DoRPC<List<string>>("listassets", payload).Result;
        }

        internal int GetNumBlocks()
        {
            return DoRPC<int>("getblockcount").Result;
        }




        // Base Implementation

        public RPC_Response<JObject> DoRPC(string MethodName, JObject Parameters = null)
        {
            return InnerDoRPC<JObject, JObject>(MethodName, Parameters);
        }

        public RPC_Response<T> DoRPC<T>(string MethodName)
        {
            return InnerDoRPC<T,object>(MethodName, null);
        }

        public RPC_Response<T> DoRPC<T>(string MethodName, JObject Parameters)
        {
            return InnerDoRPC<T, object>(MethodName, Parameters);
        }

        private RPC_Response<T> InnerDoRPC<T,U>(string MethodName, U Parameters = null) 
            where U : class
        {
            var message = new RPC_Messaage<U>()
            {
                Method = MethodName,
                Parameters = Parameters,
                Id = "1"
            };
            var json_message = JsonConvert.SerializeObject(message);

            var rpc_request = new RestRequest("", Method.POST);
            rpc_request.AddJsonBody(json_message);
            var rpc_response = RPC_Client.Execute(rpc_request);
            if(rpc_response.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError($"Got status code {rpc_response.StatusCode} for RPC call. - {rpc_response.ErrorMessage}");
                Logger.LogError($"==> {json_message}");
                Logger.LogError($"<== {rpc_response.Content}");
            }

            var response = JsonConvert.DeserializeObject<RPC_Response<T>>(rpc_response.Content);

            if(response?.Error != null)
            {
            }

            return response;
        }

        private class RPC_Messaage<T>
        {
            [JsonProperty("jsonrpc")]
            public string JsonRPC { get; set; } = "2.0";
            [JsonProperty("method")]
            public string Method { get; set; }
            [JsonProperty("params")]
            public T Parameters { get; set; }
            [JsonProperty("id")]
            public string Id { get; set; }
        }

        public class RPC_Response<T>
        {
            [JsonProperty("result")]
            public T Result { get; set; }
            [JsonProperty("error")]
            public JObject Error { get; set; }
            [JsonProperty("id")]
            public string ID { get; set; }
        }
    }
}
