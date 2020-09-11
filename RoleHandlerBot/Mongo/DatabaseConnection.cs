using MongoDB.Driver;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
namespace RoleHandlerBot.Mongo
{
    class DatabaseConnection
    {
        private static MongoClient Client;
        private static IMongoDatabase Database;
        public static string MongoUrl;
        public static string DatabaseName;

        private static void SetupConnection()
        {
            Client = new MongoClient(MongoUrl);
            Database = Client.GetDatabase(DatabaseName);
            Logger.LogInternal("Connected to Mongo Database");
        }

        public static void Init(string mongo_url)
        {
            Client = new MongoClient(mongo_url);
            Database = Client.GetDatabase(DatabaseName);
        }

        public static IMongoDatabase GetDb()
        {
            if (Client == null)
            {
                SetupConnection();
            }
            return Database;
        }

    }
}