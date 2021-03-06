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
                        var addresses = await User.GetUserAddresses(user.Id);
                        if (addresses == null)
                            continue;
                        var remove = true;
                        if (addresses != null) {
                            foreach (var address in addresses)
                                if (await Blockchain.ChainWatcher.GetBalanceOf(TokenAddress, address) >= BigInteger.Parse(GetBN())) {
                                    remove = false;
                                    break;
                                }
                        }
                        if (addresses == null) {
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as we couldn't find your verified address, please re-verify!");
                            var message = "**Follow this link to verify your ethereum address**";
                            var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address").WithDescription(message);
                            embed.WithColor(Color.DarkMagenta);
                            embed.WithUrl("https://discord.com/api/oauth2/authorize?client_id=778946094804762644&redirect_uri=https%3A%2F%2Fnft42-next.vercel.app%2F&response_type=code&scope=identify");
                            await user.SendMessageAsync(embed: embed.Build());
                        }
                        else if (remove) {
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as your token balance went below the requirement of {BigNumber.FormatUint(Requirement, tokenDecimal)} {TokenName.ToUpper()}."
                                + "To reclaim the role, please make sure to make the minimum requirement in your wallet!");
                        }
                    }
                }
                Console.WriteLine("Done\n");
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

        public static async Task<List<RoleHandler>> GetAllRolesByGuild(ulong guildId) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var roles = await (await collec.FindAsync(r => r.guildId == guildId)).ToListAsync();
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
                    Console.WriteLine("Start checking");
                    await CheckAllRolesReq();
                    await GroupHandler.CheckAllRolesReq();
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
                _ = AdminCommmands._NotifyGethUnsync();
            }
        }

        public string GetBN() {
            var str = Requirement;
            //str = str.PadRight(str.Length + tokenDecimal, '0');
            return str;
        }
    }
}
