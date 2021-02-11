using System;
using Discord;
using System.Threading;
using System.Numerics;

namespace RoleHandlerBot
{
    public class Program
    {
        public static bool IsRelease = false;
        static void Main(string[] args)
        {
            Mongo.DatabaseConnection.DatabaseName = "RoleHandlerDatabase";
            Mongo.DatabaseConnection.MongoUrl = args[0];
            Blockchain.OpenSea.api = args[4];
            if (args[1].ToLower() == "prod")
                IsRelease = true;
            else {
                Blockchain.ChainWatcher.GETH_WEB3_ENDPOINT = Blockchain.ChainWatcher.INFURA_WEB3_ENDPOINT;
            }
            //User.MigrateAllUsers().GetAwaiter().GetResult();
            //KnownOrigin.KoGraphQl.KoGraphQlQuery("0xf52393e120f918ffba50410b90a29b1f8250c879").GetAwaiter().GetResult();
            RunBot(token: args[2], prefix: args[3]);
        }
        static void RunBot(string token, string prefix)
        {
            while (true)
            {
                try
                {
                    new Bot().RunAsync(token, prefix).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    Logger.Log(new LogMessage(LogSeverity.Error, ex.ToString(), "Unexpected Exception", ex));
                    Console.WriteLine(ex.ToString());
                }
                Thread.Sleep(1000);
                break;
            }
        }
    }
}
