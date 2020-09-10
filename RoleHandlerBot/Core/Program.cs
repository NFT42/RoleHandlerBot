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
            if (args[1].ToLower() == "prod")
                IsRelease = true;
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
