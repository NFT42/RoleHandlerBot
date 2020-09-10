using System;
using System.Threading.Tasks;
using RoleHandlerBot.Mongo;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
namespace RoleHandlerBot
{
    public class User
    {
        public ulong id;
        public string address;
        public User(ulong _id, string _address)
        {
            id = _id;
            address = _address;
        }

        public static async Task LogUser(ulong id, string address)
        {
            var collec = DatabaseConnection.GetDb().GetCollection<User>("Users");
            var user = (await collec.FindAsync(u => u.id == id)).FirstOrDefault();
            if (user == null)
            {
                await collec.InsertOneAsync(new User(id, address));
            }
            else
            {
                await collec.FindOneAndReplaceAsync(u => u.id == id, new User(id, address));
            }
        }

        public static async Task<List<User>> GetAllUsers()
        {
            var collec = DatabaseConnection.GetDb().GetCollection<User>("Users");
            var list = await collec.FindAsync(u => true);
            return await list.ToListAsync();
        }

        public static async Task<User> GetUser(ulong id)
        {
            var collec = DatabaseConnection.GetDb().GetCollection<User>("Users");
            var user = (await collec.FindAsync(u => u.id == id)).FirstOrDefault();
            return user;
        }

        public static async Task<string> GetUserAddress(ulong id)
        {
            var user = await GetUser(id);
            if (user == null)
                return "";
            return (user).address;
        }
    }
}
