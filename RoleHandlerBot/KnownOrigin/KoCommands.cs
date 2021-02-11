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

namespace RoleHandlerBot.KnownOrigin {
    [Group("ko")]
    public class KoCommands : ModuleBase {

        public const ulong KO_GUILD_ID = 671350749468557312;

        public KoCommands() {
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

        public bool IfInKoServer() => Context.Guild.Id == KO_GUILD_ID;

        [Command("verify", RunMode = RunMode.Async)]
        public async Task Verify() {
            var message = "**Follow this link to verify your ethereum address**";
            var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address").WithDescription(message);
            embed.WithColor(Color.DarkMagenta);
            embed.WithUrl("https://discord.com/api/oauth2/authorize?client_id=778946094804762644&redirect_uri=https%3A%2F%2Fnft42-next.vercel.app%2F&response_type=code&scope=identify");
            await Context.Message.Author.SendMessageAsync(embed: embed.Build());
        }

        [Command("claim", RunMode = RunMode.Async)]
        public async Task Claim() {
            if (!IfInKoServer())
                return;
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            await ClaimKoProfileRole(addresses, Context.Message.Author);
            await ClaimKoUserRole(addresses, Context.Message.Author);
            await ClaimeKoArtistRole(addresses, Context.Message.Author);
            await ClaimKoWhaleRole(addresses, Context.Message.Author);
            var nftRoles = await NFTRoleHandler.GetAllRolesByGuild(Context.Guild.Id);
            foreach (var role in nftRoles) {
                var eligible = false;
                foreach (var add in addresses) {
                    switch (role.RequirementType) {
                        case NFTReqType.HoldX:
                            eligible = await Blockchain.ChainWatcher.GetBalanceOf(role.NFTAddress, add) >= role.HoldXValue;
                            break;
                        case NFTReqType.InRange:
                            eligible = (await Blockchain.OpenSea.CheckNFTInRange(add, role.NFTAddress, role.MinRange, role.MaxRange, role.HoldXValue));
                            break;
                        case NFTReqType.Custom:
                            break;
                    }
                    if (eligible)
                        break;
                }
                if (eligible) {
                    var user = Context.Message.Author as SocketGuildUser;
                    var aRole = Context.Guild.GetRole(role.RoleId);
                    try {
                        await user.AddRoleAsync(aRole);
                    }
                    catch (Exception e) { Console.WriteLine(e.Message); }
                }
            }
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }


        [Command("claimfor", RunMode = RunMode.Async)]
        public async Task ClaimFor(ulong userId) {
            if (!IfInKoServer())
                return;
            if (Context.Message.Author.Id != 195567858133106697)
                return;
            var addresses = await User.GetUserAddresses(userId);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var user = await Context.Guild.GetUserAsync(userId);
            
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            await ClaimKoProfileRole(addresses, user);
            await ClaimKoUserRole(addresses, user);
            await ClaimeKoArtistRole(addresses, user);
            await ClaimKoWhaleRole(addresses, user);
            var nftRoles = await NFTRoleHandler.GetAllRolesByGuild(Context.Guild.Id);
            foreach (var role in nftRoles) {
                var eligible = false;
                foreach (var add in addresses) {
                    switch (role.RequirementType) {
                        case NFTReqType.HoldX:
                            eligible = await Blockchain.ChainWatcher.GetBalanceOf(role.NFTAddress, add) >= role.HoldXValue;
                            break;
                        case NFTReqType.InRange:
                            eligible = (await Blockchain.OpenSea.CheckNFTInRange(add, role.NFTAddress, role.MinRange, role.MaxRange, role.HoldXValue));
                            break;
                        case NFTReqType.Custom:
                            break;
                    }
                    if (eligible)
                        break;
                }
                if (eligible) {
                    var aRole = Context.Guild.GetRole(role.RoleId);
                    try {
                        await user.AddRoleAsync(aRole);
                    }
                    catch (Exception e) { Console.WriteLine(e.Message); }
                }
            }
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        public async Task ClaimKoUserRole(List<string> addresses, IUser user) {
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var koRole = Context.Guild.GetRole(807686850718203914);
            await (user as SocketGuildUser).AddRoleAsync(koRole);
        }

        public async Task ClaimKoProfileRole(List<string> addresses, IUser user) {
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var koRole = Context.Guild.GetRole(807664717396443146);
            foreach (var add in addresses) {
                if (await CallKoAPIProfile(add)) {
                    await (user as SocketGuildUser).AddRoleAsync(koRole);
                    return;
                }
            }
        }

        public static async Task<bool> CallKoAPIProfile(string add) {
            var data = "";
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    data = await wc.DownloadStringTaskAsync("https://us-central1-known-origin-io.cloudfunctions.net/main/api/network/1/accounts/" + add + "/profile/tiny");
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            var json = JObject.Parse(data);
            return json.Count > 1;
        }

        public async Task ClaimeKoArtistRole(List<string> addresses, IUser user) {
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var artistRole = Context.Guild.GetRole(727165006524842042);
            var newArtistRole = Context.Guild.GetRole(807968579932389376);
            foreach (var add in addresses) {
                if (await Blockchain.ChainWatcher.CheckIfKoArtist(add)) {
                    if (await CheckIfNewArtist(add))
                        await (user as SocketGuildUser).AddRoleAsync(newArtistRole);
                    await (user as SocketGuildUser).AddRoleAsync(artistRole);
                    return;
                }
            }
        }

        public static async Task<bool> CheckIfNewArtist(string add) {
            var data = "";
            using (System.Net.WebClient wc = new System.Net.WebClient()) {
                try {
                    data = await wc.DownloadStringTaskAsync("https://us-central1-known-origin-io.cloudfunctions.net/main/api/network/1/accounts/" + add + "/profile/simple");
                }
                catch (Exception e) {
                    Console.WriteLine(e.Message);
                    return false;
                }
            }
            var json = JObject.Parse(data);
            if (json.Count == 1)
                return false;
            if (json["enabledTimestamp"] == null)
                return false;
            var enabledTS = (int)(((long)json["enabledTimestamp"]) / 1000);
            var now = Convert.ToInt32(((DateTimeOffset)(DateTime.UtcNow)).ToUnixTimeSeconds());
            var delta = now - enabledTS;
            var t = 90 * 24 * 3600;
            return delta < t;
        }

        public async Task ClaimKoWhaleRole(List<string> addresses, IUser user) {
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var whaleRole = Context.Guild.GetRole(807664934511050762);
            foreach (var add in addresses) {
                if (await KoGraphQl.KoGraphQlQuery(add) >= 10f) {
                    await (user as SocketGuildUser).AddRoleAsync(whaleRole);
                    return;
                }
            }
        }
    }
}
