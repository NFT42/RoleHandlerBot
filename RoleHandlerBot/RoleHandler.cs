﻿using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using System.Threading.Tasks;
using System.Numerics;
using RoleHandlerBot.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
namespace RoleHandlerBot
{
    public class RoleHandler
    {
        public ObjectId id;
        public ulong guildId;
        public ulong RoleId;
        public string TokenAddress;
        public string TokenName;
        public int Requirement;
        public int tokenDecimal;
        public string ClaimName;

        public RoleHandler(ulong gId, ulong rId, string tAd, int req, int dec, string cName, string tName) {
            id = ObjectId.GenerateNewId();
            guildId = gId;
            RoleId = rId;
            TokenAddress = tAd;
            Requirement = req;
            tokenDecimal = dec;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
        }

        public void Update(ulong gId, ulong rId, string tAd, int req, int dec, string cName, string tName) {
            guildId = gId;
            RoleId = rId;
            TokenAddress = tAd;
            Requirement = req;
            tokenDecimal = dec;
            ClaimName = cName.ToLower();
            TokenName = tName.ToUpper();
        }


        public async Task CheckAllRoleReq()
        {
            var guild = Bot.DiscordClient.GetGuild(guildId) as IGuild;
            var role = guild.GetRole(RoleId);
            var roleUsers = (await guild.GetUsersAsync());
            foreach (var user in roleUsers) {
                if (user.RoleIds.Contains(role.Id))
                {
                    if (await Blockchain.ChainWatcher.GetBalanceOf(TokenAddress, await User.GetUserAddress(user.Id)) < BigInteger.Parse(GetBN()))
                    {
                        await user.RemoveRoleAsync(role);
                        await user.SendMessageAsync($"Hello!\nYour role `{role.Name}` in the `{guild.Name}` was removed as your token balance went below the requirement of {GetBN()} {TokenName.ToUpper()}."
                            + "To reclaim the role, please make sure to make the minimum requirement in your wallet!");
                    }
                }
            }
        }

        public static async Task AddRoleHandler(ulong guildId, ulong roleId, string token, int req, int dec, string cName, string tName) {
            var collec = DatabaseConnection.GetDb().GetCollection<RoleHandler>("Roles");
            var role = (await collec.FindAsync(r => r.RoleId == roleId)).FirstOrDefault();

            if (role == null) {
                await collec.InsertOneAsync(new RoleHandler(guildId, roleId, token, req, dec, cName, tName));
            }
            else {
                role.Update(guildId, roleId, token, req, dec, cName, tName);
                await collec.ReplaceOneAsync(r => r.RoleId == roleId, role);
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
                    await Task.Delay(1000 * 3600 * 24);
                }
            }
            catch (Exception e){
                Logger.Log("Run check error : " + e.Message);
            }

        }


        public static async Task RunChecks() {
            _ = RunDailyChecks();
        }

        public string GetBN() {
            var str = Requirement.ToString();
            str = str.PadRight(str.Length + tokenDecimal, '0');
            return str;
        }
    }
}
