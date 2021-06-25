using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Numerics;
using System.IO;
using System.Text;
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

        public static async Task NotifyGethUnsync() {
            _ = _NotifyGethUnsync();
        }

        public static async Task _NotifyGethUnsync() {
            while (true) {
                if (!await Blockchain.ChainWatcher.IsGethSynced()) {
                    var user = Bot.GetUser(195567858133106697);
                    await user.SendMessageAsync("Geth node is unsynced");
                    break;
                }
                await Task.Delay(60000 * 5);
            }
        }

        [Command("dump", RunMode = RunMode.Async)]
        public async Task Dump() {
            if (!await IsAdmin()) {
                await ReplyAsync("Not admin");
                return;
            }
            try {
                await Context.Message.Author.SendFileAsync("log.txt");
            }
            catch (Exception e) {
                await ReplyAsync(e.Message);
            }
        }

        [Command("unbind", RunMode = RunMode.Async)]
        public async Task WhoIs(string address) {
            if (!await IsAdmin())
                return;
            try {
                await User.DeleteFromAddress(address);
                await ReplyAsync($"done");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                await ReplyAsync(e.Message);
            }
        }

        [Command("blocks", RunMode = RunMode.Async)]
        public async Task CheckBlocks() {
            if (!await IsAdmin())
                return;
            try {
                var (b1, b2) = await Blockchain.ChainWatcher.GetBlocks();
                await ReplyAsync($"Infura block = {b1}\nGeth block = {b2}");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
                await ReplyAsync(e.Message);
            }
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

        [Command("balanceof", RunMode = RunMode.Async)]
        public async Task bOf(ulong id, string contract) {
            try {
                // var user = await Context.Guild.GetUserAsync(id);
                var addresses = await User.GetUserAddresses(id);
                foreach (var add in addresses)
                    await ReplyAsync($"User has {await Blockchain.ChainWatcher.GetBalanceOf(contract, add)}");
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
            var list = "";
            foreach (var u in snapshotList)
                list += $"{u.addresses[0]}\n";
            byte[] byteArray = Encoding.UTF8.GetBytes(list);
            //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
            MemoryStream stream = new MemoryStream(byteArray);

            await Context.Message.Channel.SendFileAsync(stream, "snapshot.csv");
        }
    }
}
