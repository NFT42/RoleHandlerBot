using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Numerics;
using MongoDB.Driver;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Newtonsoft.Json.Linq;
using RoleHandlerBot.Mongo;
namespace RoleHandlerBot {
    public class AdminCommmands : ModuleBase {

        public AdminCommmands() {
        }

        public async Task<bool> IsAdmin() {
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return false;
            }
            var user = Context.Message.Author as IGuildUser;
            if (user.Id == 195567858133106697)
                return true;
            var roleIDs = user.RoleIds;
            foreach (var roleID in roleIDs) {
                var role = Context.Guild.GetRole(roleID);
                if (role.Permissions.Administrator)
                    return true;
                if (role.Name == "Avastars Corp" || role.Name.ToLower().Contains("admin") || role.Name.ToLower().Contains("mod"))
                    return true;
            }
            return false;
        }

        [Command("whois", RunMode = RunMode.Async)]
        public async Task WhoIs(ulong id) {
            try {
                // var user = await Context.Guild.GetUserAsync(id);
                var user = await Bot.DiscordClient.Rest.GetUserAsync(id);
                await ReplyAsync($"{user.Username}\n{user.Discriminator}");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                await ReplyAsync(e.Message);
            }
        }

        [Command("snapshot", RunMode = RunMode.Async)]
        public async Task GetSnapshotOfGuild() {
            if (!await IsAdmin())
                return;
            var guild = await Bot.DiscordClient.Rest.GetGuildAsync(Context.Guild.Id);
            var users = (await (guild.GetUsersAsync()).FlattenAsync()).ToList();
            var verifiedUsers = (await User.GetAllUsers());
            var snapshotList = new List<User>();
            foreach (var user in users) {
                var parsedUSer = verifiedUsers.FirstOrDefault(u => u.id == user.Id);
                if (parsedUSer != null)
                    snapshotList.Add(parsedUSer);
            }
            var snapshotCollec = DatabaseConnection.GetDb().GetCollection<User>("YGGSnapshot");
            await snapshotCollec.InsertManyAsync(snapshotList);
            await ReplyAsync("Snapshot taken");
        }
    }
}
