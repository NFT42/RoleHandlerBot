using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RoleHandlerBot.KnownOrigin {
    public class KoGraphQl {
        public KoGraphQl() {
        }
        public static async Task<float> KoGraphQlQuery(string address) {
            try {
                string content = "{\"query\":\"{\\n  mostEth: collector(id:  \\\"$ADDRESS\\\") {\\n    id\\n    address\\n    firstSeen\\n    firstPurchaseTimeStamp\\n    primaryPurchaseCount\\n    primaryPurchaseEthSpent\\n  }\\n}\",\"variables\":{}}";

                //content = content.Replace("\n", "\\n");
                content = content.Replace("$ADDRESS", address);
                var req = new HttpRequestMessage {
                    RequestUri = new Uri("https://api.thegraph.com/subgraphs/name/knownorigin/known-origin"),
                    Method = HttpMethod.Post,
                    Headers =
                        {
                        { HttpRequestHeader.Accept.ToString(), "application/json"},
                        { HttpRequestHeader.ContentType.ToString(), "application/json"}
                    },
                    Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json")
                };
                using (var client = new HttpClient()) {
                    var response = await client.SendAsync(req);
                    var receive = await response.Content.ReadAsStringAsync();
                    var jtoken = JObject.Parse(receive);
                    var f = jtoken["data"]["mostEth"].HasValues;
                    var j = jtoken["data"].HasValues;
                    if (!jtoken["data"]["mostEth"].HasValues)
                        return (0);
                    else
                        return (float)jtoken["data"]["mostEth"]["primaryPurchaseEthSpent"];
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                return 0;
            }
        }
    }
}
