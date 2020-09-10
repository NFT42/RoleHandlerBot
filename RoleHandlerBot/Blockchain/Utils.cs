using System;
using Nethereum.Signer;
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
    }
}
