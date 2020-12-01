using System;
using System.Threading.Tasks;
using RoleHandlerBot.Mongo;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Driver;
namespace RoleHandlerBot
{
    public class OldUser {
        public ulong id;
        public string address;
        public OldUser(ulong _id, string _address) {
            id = _id;
            address = _address;
        }
    }
    
    public class User
    {
        public ulong id;
        public List<string> addresses;
        public User(ulong _id, List<string> _address)
        {
            id = _id;
            addresses = _address;
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

        public static async Task<List<string>> GetUserAddresses(ulong id)
        {
            var user = await GetUser(id);
            if (user == null)
                return new List<string>();
            return user.addresses;
        }

        public static async Task MigrateAllUsers() {
            var collec = DatabaseConnection.GetDb().GetCollection<OldUser>("Users");
            var users = (await collec.FindAsync(u => true)).ToList();
            await collec.DeleteManyAsync(u => true);
            var newUsers = new List<User>();
            foreach (var user in users)
                newUsers.Add(new User(user.id, new List<string>() { user.address}));
            var collecNew = DatabaseConnection.GetDb().GetCollection<User>("Users");
            await collecNew.InsertManyAsync(newUsers);
        }

        public static async Task ToLowerCapAndRemoveDups() {
            var collec = DatabaseConnection.GetDb().GetCollection<User>("Users");
            var users = await GetAllUsers();
            foreach (var user in users) {
                for (int i = 0; i < user.addresses.Count; i++)
                    user.addresses[i] = user.addresses[i].ToLower();
                if (user.addresses.Distinct().Count() < user.addresses.Count) {
                    Console.WriteLine($"Dups found for {user.id}");
                    user.addresses = user.addresses.Distinct().ToList();
                }
                var update = Builders<User>.Update.Set(a => a.addresses, user.addresses);
                await collec.UpdateOneAsync(u => u.id == user.id, update);
            }
        }
    }
}
