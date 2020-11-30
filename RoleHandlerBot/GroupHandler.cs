using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Numerics;
using System.Linq.Expressions;
using RoleHandlerBot.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;

namespace RoleHandlerBot {

    public class GroupRole {
        public string ClaimName;
        public string Requirement;
    }

    public class GroupHandler {

        public ObjectId id;
        public ulong guildId;
        public string TokenAddress;
        public string TokenName;
        public int TokenDecimal;
        public string GroupName;
        public Dictionary<string, GroupRole> RoleDict;

        public GroupHandler(ulong gId, string ken, int dec, string gName, string tName) {
            id = ObjectId.GenerateNewId();
            guildId = gId;
            TokenAddress = ken.ToLower();
            TokenDecimal = dec;
            GroupName = gName.ToLower();
            TokenName = tName.ToUpper();
            RoleDict = new Dictionary<string, GroupRole>();
        }

        public async Task CheckAllRoleReq() {
            try {
                var guild = Bot.DiscordClient.GetGuild(guildId) as IGuild;
                var rolesId = RoleDict.Keys.Select(k => ulong.Parse(k)).ToList();
                var roles = new List<IRole>();
                foreach (var id in rolesId)
                    roles.Add(guild.GetRole(id));
                var roleUsers = (await guild.GetUsersAsync());
                foreach (var user in roleUsers) {
                    if (!user.IsBot) {
                        var addresses = await User.GetUserAddresses(user.Id);
                        if (addresses == null)
                            continue;
                        var balance = BigInteger.Zero;
                        var usedBalance = BigInteger.Zero;
                        foreach (var address in addresses)
                            balance += await Blockchain.ChainWatcher.GetBalanceOf(TokenAddress, address);
                        foreach (var id in rolesId)
                            if (user.RoleIds.Contains(id))
                                usedBalance += BigInteger.Parse(RoleDict[id.ToString()].Requirement);
                        var remove = usedBalance > balance;
                        if (remove) {
                            await user.RemoveRolesAsync(roles);
                            await user.SendMessageAsync($"Hello!\nYour roles related to the `{GroupName}` group in the `{guild.Name}` guild were removed as your ${TokenName.ToUpper()} balance went below the requirement to hold your current roles ."
                                + "To reclaim the roles, please make sure to make the minimum requirement in your wallet!\n"
                                + "Alternatively, you may select fewer roles to make sure you pass requirements");
                        }
                    }
                }
                Console.WriteLine("Done\n");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        public void Update(ulong gId, string tAd, int dec, string cName, string tName) {
            guildId = gId;
            TokenAddress = tAd;
            TokenDecimal = dec;
            GroupName = cName.ToLower();
            TokenName = tName.ToUpper();
        }

        public void UpdateRole(ulong roleId, string roleName, string req) {
            var role = RoleDict[roleId.ToString()];
            role.Requirement = BigNumber.ParseValueToTokenDecimal(req, TokenDecimal);
            role.ClaimName = roleName.ToLower();
        }

        public static async Task AddGroupHandler(ulong guildId, string token, int dec, string gName, string tName) {
            var collec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            var group = (await collec.FindAsync(r => r.guildId == guildId && r.TokenAddress == token.ToLower())).FirstOrDefault();

            if (group == null) {
                await collec.InsertOneAsync(new GroupHandler(guildId, token, dec, gName, tName));
            }
            else {
                group.Update(guildId, token, dec, gName, tName);
                await collec.ReplaceOneAsync(r => r.guildId == guildId && r.TokenAddress == token.ToLower(), group);
            }
        }

        public static async Task RemoveGroupHandler(ulong guildId, string gName) {
            var collec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            await collec.DeleteOneAsync(r => r.guildId == guildId && r.GroupName == gName.ToLower());
        }

        public static async Task<GroupHandler> GetGroupHandler(ulong guildId, string gName) {
            var collec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            var group = (await collec.FindAsync(r => r.guildId == guildId && r.GroupName == gName.ToLower())).FirstOrDefault();
            return group;
        }

        public static async Task<List<GroupHandler>> GetAllGroupHandlerFromGuild(ulong guildId) {
            var collec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            var groups = (await collec.FindAsync(r => r.guildId == guildId)).ToList();
            return groups;
        }

        public static async Task<(KeyValuePair<string, GroupRole>, GroupHandler)> GetGroupRoleFromClaimName(string claimName, ulong guildId) {
            var groups = await GetAllGroupHandlerFromGuild(guildId);
            foreach (var group in groups) {
                foreach (var pair in group.RoleDict) {
                    if (pair.Value.ClaimName == claimName.ToLower())
                        return (pair, group);
                }
            }
            return (new KeyValuePair<string, GroupRole>(), null);
        }

        public async Task AddRole(ulong roleId, string roleName, string requirement) {
            if (RoleDict.ContainsKey(roleId.ToString())) 
                UpdateRole(roleId, roleName, requirement);
            else
                RoleDict.Add(roleId.ToString(), new GroupRole() {
                    ClaimName = roleName.ToLower(),
                    Requirement = BigNumber.ParseValueToTokenDecimal(requirement, TokenDecimal)
                });
            await UpdateOne(g => g.RoleDict, RoleDict);
        }

        public async Task UpdateRole(string roleName, string requirement) {
            foreach (var role in RoleDict.Values) {
                if (role.ClaimName == roleName.ToLower())
                    role.Requirement = BigNumber.ParseValueToTokenDecimal(requirement, TokenDecimal);
            }
            await UpdateOne(g => g.RoleDict, RoleDict);
        }

        public async Task RemoveRole(ulong roleId) {
            if (RoleDict.ContainsKey(roleId.ToString())) {
                RoleDict.Remove(roleId.ToString());
                await UpdateOne(g => g.RoleDict, RoleDict);
            }
        }

        public async Task UpdateOne<T>(Expression<Func<GroupHandler, T>> expr, T value) {
            var accountCollec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            var update = Builders<GroupHandler>.Update.Set(expr, value);
            await accountCollec.FindOneAndUpdateAsync(a => a.id == id, update);
        }

        public static async Task CheckAllRolesReq() {
            var collec = DatabaseConnection.GetDb().GetCollection<GroupHandler>("Groups");
            var groups = await (await collec.FindAsync(r => true)).ToListAsync();
            foreach (var group in groups)
                await group.CheckAllRoleReq();
        }
    }
}
