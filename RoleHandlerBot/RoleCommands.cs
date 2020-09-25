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
namespace RoleHandlerBot
{
    public class RoleCommands : ModuleBase
    {
        public async Task<bool> IsAdmin()
        {
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
                if (role.Name == "Avastars Corp" || role.Name.ToLower().Contains("admin") || role.Name.ToLower().Contains("mod"))
                    return true;
            }
            return false;
        }

        [Command("help")]
        public async Task GetHelp() {
            var embed = new EmbedBuilder().WithTitle("❓ Help ❓").WithColor(Color.DarkRed);
            embed.AddField("Add a role [Admin]", "Use command `!addrole @role tokenName tokenAddress requirement decimal claimName` to add a role");
            embed.AddField("Update a role [Admin]", "Use command `!updaterole @role requirement` to update a role rerquirement");
            embed.AddField("Remove a role [Admin]", "Use command `!deleterole @role` to remove a role");
            embed.AddField("Show all roles", "Use command `!showroles` to get a list of all roles");
            embed.AddField("Attach an address", "Use command `!verify address` and paste result from web app");
            embed.AddField("Claim a role", "Use command `!claim claimName` to claim a role if you meet requirements");
            await ReplyAsync(embed: embed.Build());

        }

        [Command("AddRole", RunMode = RunMode.Async)]
        public async Task AddRole(IRole role, string tokenName, string token, string req, int dec, string name)
        {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null)
            {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            if (BigNumber.IsValidValue(req, dec)) {
                await RoleHandler.AddRoleHandler(Context.Guild.Id, role.Id, token, req, dec, name, tokenName);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await ReplyAsync("Wrong token value in respect to decimals");
        }

        [Command("updaterole", RunMode = RunMode.Async)]
        public async Task UpdateRole(IRole role, string req) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await RoleHandler.UpdateRoleHandler(Context.Guild.Id, role.Id, req);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("DeleteRole", RunMode = RunMode.Async)]
        public async Task Dlete(IRole role)
        {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null)
            {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await RoleHandler.RemoveRoleHandler(role.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("ens", RunMode = RunMode.Async)]
        public async Task TestRun(string ens) {
            await ReplyAsync(await Blockchain.Utils.GetENSAddress(ens));
            //await RoleHandler.CheckAllRolesReq();
            //Console.WriteLine("");
        }

        [Command("showRoles", RunMode = RunMode.Async)]
        public async Task ShowRoles()
        {
            var roles = await RoleHandler.GetAllRoles();
            roles = roles.Where(r => r.guildId == Context.Guild.Id).ToList();
            var embed = new EmbedBuilder().WithTitle("📜 Roles 📜").WithColor(Color.Blue);
            embed.WithDescription("Delete a role handler using `!deleteRole @role` [ADMIN ONLY]");

            int i = 1;
            foreach (var role in roles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                embed.AddField($"{i}. Requirement: {BigNumber.FormatUint(role.Requirement, role.tokenDecimal)} {role.TokenName}", $"{mention} | type `!claim {role.ClaimName}` to claim");
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("claim")]
        public async Task ClaimeRole(string claim)
        {
            var role = await RoleHandler.GetRoleByClaimName(claim);
            if (role == null)
                return;
            if (Context.Guild == null || Context.Guild.Id != role.guildId)
            {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var add = await User.GetUserAddress(Context.Message.Author.Id);
            if (add.Length == 0) {
                await ReplyAsync("User has not binded an address. Please Bind an address using command `!verify your_address` example `!verify 0x123456789abcdefABCDEF9876543210123456789`");
                return;
            } 
            if (await Blockchain.ChainWatcher.GetBalanceOf(role.TokenAddress, add) >= BigInteger.Parse(role.GetBN()))
            {
                var user = Context.Message.Author as SocketGuildUser;
                await user.AddRoleAsync(Context.Guild.GetRole(role.RoleId));
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        [Command("verify")]
        public async Task BindWallet(string address, string hash = "")
        {
            if (hash.Length == 0)
            {
                if (address.EndsWith(".eth")) {
                    address = await Blockchain.Utils.GetENSAddress(address);
                    if (address == null || address == "0x0000000000000000000000000000000000000000") {
                        await ReplyAsync("Error: could not find ens domain.");
                        return;
                    }
                }
                var message = "**Paste back the value copied on your clipboard here!\nThe value will be obtained by signing a message on the website**";
                var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address").WithDescription(message);
                embed.WithColor(Color.DarkMagenta);
                embed.WithUrl("https://cesarsld.github.io/NFT42VerifyPage/?" + $"discordId={Context.Message.Author.Id}&address={address}");
                await Context.Message.Author.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                if (Blockchain.Utils.IsSignatureValid(hash, address, $"DiscordId = {Context.Message.Author.Id}\nUserAddress = {address}"))
                {
                    await User.LogUser(Context.Message.Author.Id, address);
                    await ReplyAsync("Binded address to Discord account!");
                }
                else
                {
                    await ReplyAsync("Error: wrong signature!");
                }
            }
        }

    }
}
