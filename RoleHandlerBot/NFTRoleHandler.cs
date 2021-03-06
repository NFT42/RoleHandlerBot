using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Numerics;
using RoleHandlerBot.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace RoleHandlerBot {

    public enum NFTReqType {
        HoldX,
        InRange,
        Custom
    }

    public class NFTRoleHandler {

        public static bool Running = false;

        public ObjectId id;
        public ulong guildId;
        public ulong RoleId;
        public string NFTAddress;
        public string TokenName;
        public NFTReqType RequirementType;
        public int HoldXValue;
        public int MinRange;
        public int MaxRange;
        public string CustomCall;
        public string ClaimName;

        public NFTRoleHandler() {
        }

        public NFTRoleHandler(ulong gId, ulong rId, string tAd, NFTReqType type, string cName, string tName, int holdX, int min, int max, string custom = "") {
            id = ObjectId.GenerateNewId();
            guildId = gId;
            RoleId = rId;
            NFTAddress = tAd;
            RequirementType = type;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
            HoldXValue = holdX;
            MinRange = min;
            MaxRange = max;
            CustomCall = custom;
        }

        public void Update(ulong gId, ulong rId, string tAd, NFTReqType type, string cName, string tName, int holdX, int min, int max, string custom = "") {
            guildId = gId;
            RoleId = rId;
            NFTAddress = tAd;
            RequirementType = type;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
            HoldXValue = holdX;
            MinRange = min;
            MaxRange = max;
            CustomCall = custom;
        }

        public void Update(NFTReqType type, int holdX, int min, int max, string custom = "") {
            RequirementType = type;
            HoldXValue = holdX;
            MinRange = min;
            MaxRange = max;
            CustomCall = custom;
        }

        public async Task CheckAllRoleReqTest(ulong id) {
            try {
                var guild = Bot.DiscordClient.GetGuild(guildId) as IGuild;
                var role = guild.GetRole(RoleId);
                var roleUsers = (await guild.GetUsersAsync());
                var list = roleUsers.Where(u => u.Id == id).ToList();
                Console.WriteLine($"Checking requirements for {role.Name}");
                foreach (var user in list) {
                    if (user.RoleIds.Contains(role.Id) && !user.IsBot) {
                        bool qualifies = true;
                        var ownerAddresses = await User.GetUserAddresses(user.Id);
                        if (ownerAddresses != null) {
                            foreach (var ownerAddress in ownerAddresses) {
                                switch (RequirementType) {
                                    case NFTReqType.HoldX:
                                        qualifies = await Blockchain.ChainWatcher.GetBalanceOf(NFTAddress, ownerAddress) >= HoldXValue;
                                        break;
                                    case NFTReqType.InRange:
                                        qualifies = (await Blockchain.OpenSea.CheckNFTInRange(ownerAddress, NFTAddress, MinRange, MaxRange, HoldXValue));
                                        break;
                                    case NFTReqType.Custom:
                                        qualifies = true;
                                        break;
                                }
                                if (qualifies)
                                    break;
                            }
                        }
                        if (ownerAddresses == null) {
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as we couldn't find your verified address, please re-verify!");
                            var message = "**Follow this link to verify your ethereum address**";
                            var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address").WithDescription(message);
                            embed.WithColor(Color.DarkMagenta);
                            embed.WithUrl("https://discord.com/api/oauth2/authorize?client_id=778946094804762644&redirect_uri=https%3A%2F%2Fnft42-next.vercel.app%2F&response_type=code&scope=identify");
                            await user.SendMessageAsync(embed: embed.Build());
                        }
                        else if (!qualifies) {
                            //Console.WriteLine("mmh");
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as your {TokenName} requirement did not meet expectations."
                                + "To reclaim the role, please make sure to make the minimum requirement in your wallet!");
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public async Task CheckAllRoleReq() {
            try {
                var guild = Bot.DiscordClient.GetGuild(guildId) as IGuild;
                var role = guild.GetRole(RoleId);
                var roleUsers = (await guild.GetUsersAsync());
                Console.WriteLine($"Checking requirements for {role.Name}");
                foreach (var user in roleUsers) {
                    if (user.RoleIds.Contains(role.Id) && !user.IsBot) {
                        bool qualifies = true;
                        var ownerAddresses = await User.GetUserAddresses(user.Id);
                        foreach (var ownerAddress in ownerAddresses) {
                            switch (RequirementType) {
                                case NFTReqType.HoldX:
                                    qualifies = await Blockchain.ChainWatcher.GetBalanceOf(NFTAddress, ownerAddress) >= HoldXValue;
                                    break;
                                case NFTReqType.InRange:
                                    qualifies = (await Blockchain.OpenSea.CheckNFTInRange(ownerAddress, NFTAddress, MinRange, MaxRange, HoldXValue));
                                    break;
                                case NFTReqType.Custom:
                                    qualifies = true;
                                    break;
                            }
                            if (qualifies)
                                break;
                        }
                        if (!qualifies) {
                            await user.RemoveRoleAsync(role);
                            await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as your {TokenName} requirement did not meet expectations."
                                + "To reclaim the role, please make sure to make the minimum requirement in your wallet!");
                        }
                    }
                }
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public static async Task AddRoleHandler(ulong guildId, ulong roleId, string token, NFTReqType type, string cName, string tName, int holdX, int min, int max, string custom = "") {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();

            if (role == null) {
                await collec.InsertOneAsync(new NFTRoleHandler(guildId, roleId, token, type, cName, tName, holdX, min, max, custom));
            }
            else {
                role.Update(guildId, roleId, token, type, cName, tName, holdX, min, max, custom);
                await collec.ReplaceOneAsync(r => r.RoleId == roleId, role);
            }
        }

        public static async Task UpdateRoleHandler(ulong guildId, ulong roleId, NFTReqType type, int holdX, int min, int max, string custom = "") {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();

            if (role == null) {
                return;
            }
            else {
                role.Update(type, holdX, min, max , custom);
                var update = Builders<NFTRoleHandler>.Update.Set(r => r.RequirementType, role.RequirementType)
                    .Set(r => r.HoldXValue, role.HoldXValue)
                    .Set(r => r.MinRange, role.MinRange)
                    .Set(r => r.MaxRange, role.MaxRange)
                    .Set(r => r.CustomCall, role.CustomCall); ;
                await collec.UpdateOneAsync(r => r.RoleId == role.RoleId, update);
            }
        }

        public static async Task RemoveRoleHandler(ulong roleId) {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            await collec.DeleteOneAsync(r => r.RoleId == roleId);
        }

        public static async Task<NFTRoleHandler> GetRolehandler(ulong roleId) {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();
            return role;
        }

        public static async Task<NFTRoleHandler> GetRoleByClaimName(string claim, ulong guildId) {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var role = (await collec.FindAsync(r => r.ClaimName == claim.ToLower() && r.guildId == guildId)).FirstOrDefault();
            return role;
        }

        public static async Task<List<NFTRoleHandler>> GetAllRoles() {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var roles = await (await collec.FindAsync(r => true)).ToListAsync();
            return roles;
        }

        public static async Task<List<NFTRoleHandler>> GetAllRolesByGuild(ulong guildId) {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var roles = await (await collec.FindAsync(r => r.guildId == guildId)).ToListAsync();
            return roles;
        }

        public static async Task CheckAllRolesReq() {
            var collec = DatabaseConnection.GetDb().GetCollection<NFTRoleHandler>("NFTRoles");
            var roles = await (await collec.FindAsync(r => true)).ToListAsync();
            foreach (var role in roles)
                await role.CheckAllRoleReq();
        }
    }
}
