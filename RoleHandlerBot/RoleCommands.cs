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
                if (role.Permissions.Administrator)
                    return true;
                if (role.Name == "Avastars Corp" || role.Name.ToLower().Contains("admin") || role.Name.ToLower().Contains("mod"))
                    return true;
            }
            return false;
        }

        [Command("help")]
        public async Task GetHelp() {
            var embed = new EmbedBuilder().WithTitle("❓ Help ❓").WithColor(Color.DarkRed);
            embed.AddField("Add a role [Admin]", "Use command `!addrole @role tokenName tokenAddress requirement decimal claimName` to add a role");
            embed.AddField("Add an NFT role [Admin]", "Use command `!addnftrole @role nftName nftAddress \"hold\" holdValue \"range\"(optional) [a;b](optional) claimName` to add an nft role\n" +
                "Example:\n`!addnftrole @yourRole AVASTAR 0xf3e778f839934fc819cfa1040aabacecba01e049 hold 2 range [200;25200] claimava`\n" +
                "`!addnftrole @nftaxie AXIE 0xf5b0a3efb8e8e4c201e2a935f110eaaf3ffecb8d hold 5 claimaxie`");
            embed.AddField("Update a role [Admin]", "Use command `!updaterole @role requirement` to update a role rerquirement");
            embed.AddField("Remove a role [Admin]", "Use command `!deleterole @role` or `!deletenftrole @role` to remove a role");
            embed.AddField("Show all roles", "Use command `!showroles` to get a list of all roles");
            embed.AddField("Attach an address", "Use command `!verify` and paste result from web app");
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

        [Command("AddNFTRole", RunMode = RunMode.Async)]
        public async Task AddNFTRole(IRole role, params string[] input) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            if (input.Length > 7)
                return;
            var tokenName = input[0];
            var nftAddress = input[1];
            var reqType = input[2];
            var value = Convert.ToInt32(input[3]);
            var claimName = input[input.Length - 1];
            var inRange = "";
            var rangeValue = "";
            NFTReqType type = NFTReqType.HoldX;
            var min = 0;
            var max = 0;
            if (reqType.ToLower().StartsWith("hold")) {
                type = NFTReqType.HoldX;

            }
            if (input.Length > 5) {
                inRange = input[4];
                rangeValue = input[5];
                if (inRange.ToLower() == "range") {
                    if (rangeValue[0] == '[' && rangeValue[rangeValue.Length - 1] == ']') {
                        rangeValue = rangeValue.Substring(1, rangeValue.Length - 2);
                        var arr = rangeValue.Split(';');
                        if (arr.Length == 2 && int.TryParse(arr[0], out min) && int.TryParse(arr[1], out max) && min < max) {
                            type = NFTReqType.InRange;
                        }
                        else {
                            await ReplyAsync("Wrong range format");
                            return;
                        }
                    }
                    else {
                        await ReplyAsync("Wrong range format");
                        return;
                    }
                }
            }
                    await NFTRoleHandler.AddRoleHandler(Context.Guild.Id, role.Id, nftAddress, type, claimName, tokenName, value, min, max);
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
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

        [Command("DeletenftRole", RunMode = RunMode.Async)]
        public async Task DeleteNFTRole(IRole role) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await NFTRoleHandler.RemoveRoleHandler(role.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("ens", RunMode = RunMode.Async)]
        public async Task TestRun(string ens) {
            await ReplyAsync(await Blockchain.Utils.GetENSAddress(ens));
        }

        [Command("showRoles", RunMode = RunMode.Async)]
        public async Task ShowRoles()
        {
            var roles = await RoleHandler.GetAllRoles();
            roles = roles.Where(r => r.guildId == Context.Guild.Id).ToList();
            var embed = new EmbedBuilder().WithTitle("📜 Roles 📜").WithColor(Color.Blue);
            embed.WithDescription("Delete a role handler using `!deleteRole @role`or `!deleteNFTRoles @role` [ADMIN ONLY]");

            int i = 1;
            foreach (var role in roles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                embed.AddField($"{i++}. Requirement: {BigNumber.FormatUint(role.Requirement, role.tokenDecimal)} {role.TokenName}", $"{mention} | type `!claim {role.ClaimName}` to claim");
            }

            var nftRoles = await NFTRoleHandler.GetAllRoles();
            nftRoles = nftRoles.Where(r => r.guildId == Context.Guild.Id).ToList();
            foreach (var role in nftRoles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                var range = role.RequirementType == NFTReqType.InRange ? $" in range [{role.MinRange};{role.MaxRange}]" : "";
                embed.AddField($"{i++}. Requirement: hold {role.HoldXValue} {role.TokenName}{range}", $"{mention} | type `!claim {role.ClaimName}` to claim");
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("claim", RunMode = RunMode.Async)]
        public async Task ClaimeRole(string claim)
        {
            var nftRole = await NFTRoleHandler.GetRoleByClaimName(claim, Context.Guild.Id);
            if (nftRole != null)
                await ClaimNFTRole(nftRole);
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
                await ReplyAsync("User has not binded an address. Please Bind an address using command `!verify`");
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

        public async Task ClaimNFTRole(NFTRoleHandler role) {
            if (Context.Guild == null || Context.Guild.Id != role.guildId) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var add = await User.GetUserAddress(Context.Message.Author.Id);
            if (add.Length == 0) {
                await ReplyAsync("User has not binded an address. Please Bind an address using command `!verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            var eligible = false;
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
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            if (eligible) {
                var user = Context.Message.Author as SocketGuildUser;
                await user.AddRoleAsync(Context.Guild.GetRole(role.RoleId));
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        [Command("verify")]
        public async Task BindWallet() {
            var message = "**Follow this link to verify your ethereum address**";
            var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address").WithDescription(message);
            embed.WithColor(Color.DarkMagenta);
            embed.WithUrl("https://discord.com/api/oauth2/authorize?client_id=778946094804762644&redirect_uri=https%3A%2F%2Fnft42-next.vercel.app%2F&response_type=code&scope=identify");
            await Context.Message.Author.SendMessageAsync(embed: embed.Build());
        }

    }
}
