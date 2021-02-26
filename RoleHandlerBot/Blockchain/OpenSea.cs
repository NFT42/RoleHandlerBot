using System;
using System.Numerics;
using System.Net;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace RoleHandlerBot.Blockchain {
    public class OpenSea {
        public static string api;
        public OpenSea() {
        }

        public static async Task<bool> CheckNFTInRange(string owner, string contractAddress, BigInteger min, BigInteger max, int toHold) {
            var balanceOf = await ChainWatcher.GetBalanceOf(contractAddress, owner);
            var pages = balanceOf / 50 + (balanceOf % 50 > 0 ? 1 : 0);
            var count = 0;
            for (int i = 0; i < pages; i++) {
                if (count >= toHold)
                    return true;
                var json = await GetOpenSeaData(owner, contractAddress, 50 * i);
                foreach (var nft in json["assets"]) {
                    var tokenId = BigInteger.Parse((string)nft["token_id"]);
                    if (tokenId >= min && tokenId <= max)
                        count++;
                }
            }
            return count >= toHold;
        }

        public static async Task<JObject> GetOpenSeaData(string owner, string contractAddress, int offset) {
            var baseUrl = "https://api.opensea.io/api/v1/assets?order_direction=asc&order_by=token_id";
            var url = baseUrl + $"&owner={owner}&asset_contract_address={contractAddress}";
            url += $"&offset={offset}";
            var req = new HttpRequestMessage {
                RequestUri = new Uri(url),
                Method = HttpMethod.Get,
                Headers = { { "X-API-KEY", api} },
            };
            using (var client = new HttpClient()) {
                var res = await client.SendAsync(req);
                var content = await res.Content.ReadAsStringAsync();
                var json = JObject.Parse(content);
                return json;
            }
        }
    }
}