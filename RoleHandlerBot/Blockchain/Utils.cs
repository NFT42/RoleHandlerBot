using System;
using System.Threading.Tasks;
using Nethereum.Web3;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.ENS;
using Nethereum.ENS.ENSRegistry.ContractDefinition;
using System.Text;

namespace RoleHandlerBot.Blockchain
{
    public class Utils
    {
        public Utils()
        {
        }

        public static bool IsSignatureValid(string sentSignature, string sentAddress, string msg)
        {
            var msgHash = Encoding.UTF8.GetBytes(msg);
            var signer = new EthereumMessageSigner();
            //var signature = signer.HashAndSign(msg, msg);
            var address = signer.EcRecover(msgHash, sentSignature);
            if (address.ToLower() == sentAddress.ToLower())
                return true;
            else
                return false;
        }

        public static async Task<string> GetENSAddress(string ensName) {
            try {
                var ensUtil = new EnsUtil();
                var contract = "0x314159265dD8dbb310642f98f50C066173C1259b"; //ENS contract address
                var web3 = new Web3(ChainWatcher.GETH_WEB3_ENDPOINT);
                var fullNameNode = ensUtil.GetNameHash(ensName);
                var ensRegistryService = new ENSRegistryService(web3, contract);
                var oldResolver = "0x226159d592E2b063810a10Ebf6dcbADA94Ed68b8";
                var resolverAddress = await ensRegistryService.ResolverQueryAsync(
                    new ResolverFunction() { Node = fullNameNode.HexToByteArray() });
                var newRes = "0x4976fb03C32e5B8cfe2b6cCB31c09Ba78EBaBa41";
                var resolverService = new PublicResolverService(web3, resolverAddress);
                var migratedResolverService = new PublicResolverService(web3, newRes);
                var address = await resolverService.AddrQueryAsync(fullNameNode.HexToByteArray());
                var migratedAddress = await migratedResolverService.AddrQueryAsync(fullNameNode.HexToByteArray());
                if (migratedAddress == "0x0000000000000000000000000000000000000000")
                    return address;
                else
                    return migratedAddress;
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
            return null;
        }
    }
}
