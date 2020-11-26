using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Numerics;
using RoleHandlerBot.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core;
namespace RoleHandlerBot
{
    public class RoleHandler
    {
        public static bool Running = false;

        public ObjectId id;
        public ulong guildId;
        public ulong RoleId;
        public string TokenAddress;
        public string TokenName;
        public string Requirement;
        public int tokenDecimal;
        public string ClaimName;

        public RoleHandler(ulong gId, ulong rId, string tAd, string req, int dec, string cName, string tName) {
            id = ObjectId.GenerateNewId();
            guildId = gId;
            RoleId = rId;
            TokenAddress = tAd;
            Requirement = req;
            tokenDecimal = dec;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
        }

        public void Update(ulong gId, ulong rId, string tAd, string req, int dec, string cName, string tName) {
            guildId = gId;
            RoleId = rId;
            TokenAddress = tAd;
            Requirement = req;
            tokenDecimal = dec;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
        }

        public void Update(string req) {
            Requirement = req;
        }

        public async Task CheckAllRoleReq()
        {
            try {
                var guild = Bot.DiscordClient.GetGuild(guildId) as IGuild;
                var role = guild.GetRole(RoleId);
                var roleUsers = (await guild.GetUsersAsync());
                Console.WriteLine($"Checking requirements for {role.Name}");
                foreach (var user in roleUsers) {
                    if (user.RoleIds.Contains(role.Id) && !user.IsBot) {
                        if (await Blockchain.ChainWatcher.GetBalanceOf(TokenAddress, await User.GetUserAddress(user.Id)) < BigInteger.Parse(GetBN())) {
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as your token balance went below the requirement of {BigNumber.FormatUint(Requirement, tokenDecimal)} {TokenName.ToUpper()}."
                                + "To reclaim the role, please make sure to make the minimum requirement in your wallet!");
                        }
                    }
                }
            }
            catch(Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task AddRoleHandler(ulong guildId, ulong roleId, string token, string req, int dec, string cName, string tName) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();

            if (role == null) {
                await collec.InsertOneAsync(new RoleHandler(guildId, roleId, token, BigNumber.ParseValueToTokenDecimal(req, dec), dec, cName, tName));
            }
            else {
                role.Update(guildId, roleId, token, req, dec, cName, tName);
                await collec.ReplaceOneAsync(r => r.RoleId == roleId, role);
            }
        }

        public static async Task UpdateRoleHandler(ulong guildId, ulong roleId, string req) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();

            if (role == null) {
                return;
            }
            else {
                if (BigNumber.IsValidValue(req, role.tokenDecimal)) {
                    role.Update(BigNumber.ParseValueToTokenDecimal(req, role.tokenDecimal));
                    var update = Builders<RoleHandler>.Update.Set(r => r.Requirement, role.Requirement);
                    await collec.UpdateOneAsync(r => r.RoleId == role.RoleId, update);
                }
            }
        }

        public static async Task RemoveRoleHandler(ulong roleId) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            await collec.DeleteOneAsync(r => r.RoleId == roleId);
        }

        public static async Task<RoleHandler> GetRolehandler(ulong roleId) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();
            return role;
        }

        public static async Task<RoleHandler> GetRoleByClaimName(string claim) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var role = (await collec.FindAsync(r => r.ClaimName == claim.ToLower())).FirstOrDefault();
            return role;
        }

        public static async Task<List<RoleHandler>> GetAllRoles() {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var roles = await (await collec.FindAsync(r => true)).ToListAsync();
            return roles;
        }

        public static async Task CheckAllRolesReq() {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var roles = await (await collec.FindAsync(r => true)).ToListAsync();
            foreach (var role in roles)
                await role.CheckAllRoleReq();
        }


        public static async Task RunDailyChecks() {
            try{
                while (true) {
                    await CheckAllRolesReq();
                    await NFTRoleHandler.CheckAllRolesReq();
                    Console.WriteLine("Done checking");
                    await Task.Delay(1000 * 3600 * 24);
                }
            }
            catch (Exception e){
                Logger.Log("Run check error : " + e.Message);
            }
        }


        public static async Task RunChecks() {
            if (!Running) {
                Running = true;
                _ = RunDailyChecks();
            }
        }

        public string GetBN() {
            var str = Requirement;
            //str = str.PadRight(str.Length + tokenDecimal, '0');
            return str;
        }
    }
}
