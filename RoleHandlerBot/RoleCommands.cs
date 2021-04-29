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
            embed.AddField("Add a role [Admin]", $"Use command `{Bot.CommandPrefix}addrole @role tokenName tokenAddress requirement decimal claimName` to add a role");
            embed.AddField("Add an NFT role [Admin]", $"Use command `{Bot.CommandPrefix}addnftrole @role nftName nftAddress \"hold\" holdValue \"range\"(optional) [a;b](optional) claimName` to add an nft role\n" +
                $"Example:\n`{Bot.CommandPrefix}addnftrole @yourRole AVASTAR 0xf3e778f839934fc819cfa1040aabacecba01e049 hold 2 range [200;25200] claimava`\n" +
                $"`{Bot.CommandPrefix}addnftrole @nftaxie AXIE 0xf5b0a3efb8e8e4c201e2a935f110eaaf3ffecb8d hold 5 claimaxie`");
            embed.AddField("Update a role [Admin]", $"Use command `{Bot.CommandPrefix}updaterole @role requirement` to update a role rerquirement");
            embed.AddField("Remove a role [Admin]", $"Use command `{Bot.CommandPrefix}deleterole @role` or `{Bot.CommandPrefix}deletenftrole @role` to remove a role");
            embed.AddField("Show all group roles [admin]", $"Use command `{Bot.CommandPrefix}grouproles` to get a list of all roles");
            embed.AddField("Create an token group [Admin]", $"`{Bot.CommandPrefix}creategroup tokenAddress tokenName tokenDecimal groupName` to create a new token group");
            embed.AddField("Create a group role [Admin]", $"Use command `{Bot.CommandPrefix}addgrouprole @role groupName tokenRequirement claimName` to create a group role");
            embed.AddField("Update a group role [Admin]", $"Use command `{Bot.CommandPrefix}updategrouprole groupName tokenRequirement claimName` to update a group role");
            embed.AddField("Remove a group role [Admin]", $"Use command `{Bot.CommandPrefix}removegroudrole groupname claimName` to remove a group role");
            embed.AddField("Show all roles", $"Use command `{Bot.CommandPrefix}showroles` to get a list of all roles");
            embed.AddField("Attach an address", $"Use command `{Bot.CommandPrefix}verify` and paste result from web app");
            embed.AddField("Claim a role", $"Use command `{Bot.CommandPrefix}claim claimName` to claim a role if you meet requirements");
            embed.AddField("Claim all roles", $"Use command `{Bot.CommandPrefix}claimall` to claim all roles if you meet requirements");
            await ReplyAsync(embed: embed.Build());

        }

        [Command("user", RunMode = RunMode.Async)]
        public async Task UserInfo(ulong id) {
            var user = await Bot.DiscordClient.Rest.GetUserAsync(id);
        }

        [Command("CreateGroup", RunMode = RunMode.Async)]
        public async Task AddGroup(string tokenAddress, string tname, int dec, string gName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await GroupHandler.AddGroupHandler(Context.Guild.Id, tokenAddress, dec, gName, tname);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("RemoveGroup", RunMode = RunMode.Async)]
        public async Task RemoveGroup(string gName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await GroupHandler.RemoveGroupHandler(Context.Guild.Id, gName);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("AddGroupRole", RunMode = RunMode.Async)]
        public async Task AddGroupRole(IRole role, string gName, string req, string cName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            var group = await GroupHandler.GetGroupHandler(Context.Guild.Id, gName);
            if (BigNumber.IsValidValue(req, group.TokenDecimal)) {
                await group.AddRole(role.Id, cName, req);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await ReplyAsync("Wrong token value in respect to decimals");
        }

        [Command("UpdateGroupRole", RunMode = RunMode.Async)]
        public async Task UpdateGroupRole(IRole role, string gName, string req, string cName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            var group = await GroupHandler.GetGroupHandler(Context.Guild.Id, gName);
            if (BigNumber.IsValidValue(req, group.TokenDecimal)) {
                await group.AddRole(role.Id, cName, req);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await ReplyAsync("Wrong token value in respect to decimals");
        }

        [Command("UpdateGroupRole", RunMode = RunMode.Async)]
        public async Task UpdateGroupRole(string gName, string req, string cName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            var group = await GroupHandler.GetGroupHandler(Context.Guild.Id, gName);
            if (BigNumber.IsValidValue(req, group.TokenDecimal)) {
                await group.UpdateRole(cName, req);
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await ReplyAsync("Wrong token value in respect to decimals");
        }

        [Command("RemoveGroupRole", RunMode = RunMode.Async)]
        public async Task RemoveGroupRole(string gName, string cName) {
            if (!await IsAdmin())
                return;
            if (Context.Guild == null) {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            var group = await GroupHandler.GetGroupHandler(Context.Guild.Id, gName);
            var groupRole = group.RoleDict.Where(r => r.Value.ClaimName == cName.ToLower()).FirstOrDefault();
            if (groupRole.Value != null) {
                await group.RemoveRole(ulong.Parse(groupRole.Key));
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
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

        [Command("showRoles", RunMode = RunMode.Async)]
        public async Task ShowRoles()
        {
            var roles = await RoleHandler.GetAllRoles();
            roles = roles.Where(r => r.guildId == Context.Guild.Id).ToList();
            var embed = new EmbedBuilder().WithTitle("📜 Roles 📜").WithColor(Color.Blue);
            embed.WithDescription($"Delete a role handler using `{Bot.CommandPrefix}deleteRole @role`or `{Bot.CommandPrefix}deleteNFTRoles @role` [ADMIN ONLY]");

            int i = 1;
            foreach (var role in roles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                embed.AddField($"{i++}. Requirement: {BigNumber.FormatUint(role.Requirement, role.tokenDecimal)} {role.TokenName}", $"{mention} | type `{Bot.CommandPrefix}claim {role.ClaimName}` to claim");
            }

            var nftRoles = await NFTRoleHandler.GetAllRoles();
            nftRoles = nftRoles.Where(r => r.guildId == Context.Guild.Id).ToList();
            foreach (var role in nftRoles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                var range = role.RequirementType == NFTReqType.InRange ? $" in range [{role.MinRange};{role.MaxRange}]" : "";
                embed.AddField($"{i++}. Requirement: hold {role.HoldXValue} {role.TokenName}{range}", $"{mention} | type `{Bot.CommandPrefix}claim {role.ClaimName}` to claim");
            }
            var groups = await GroupHandler.GetAllGroupHandlerFromGuild(Context.Guild.Id);
            foreach (var group in groups) {
                foreach (var pair in group.RoleDict) {
                    var str = $"{i++}. {group.GroupName} group:";
                    var inStr = "";
                    inStr += $" Requirement -> hold {BigNumber.FormatUint(pair.Value.Requirement, group.TokenDecimal)} {group.TokenName}";
                    var mention = Context.Guild.GetRole(ulong.Parse(pair.Key)).Mention;
                    embed.AddField(str + inStr, $"{mention} | type `{Bot.CommandPrefix}claim {pair.Value.ClaimName}` to claim");
                }
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("ShowGroups")]
        public async Task ShowGroups() {
            if (!await IsAdmin())
                return;
            var embed = new EmbedBuilder().WithTitle("📜 Groups 📜").WithColor(Color.Blue);
            embed.WithDescription($"Delete a role handler using `{Bot.CommandPrefix}deletegroup groupName` [ADMIN ONLY]");
            var groups = await GroupHandler.GetAllGroupHandlerFromGuild(Context.Guild.Id);
            var i = 1;
            foreach (var group in groups) {
                foreach (var pair in group.RoleDict) {
                    var str = $"{i++}. {group.GroupName} group:";
                    var inStr = "";
                    inStr += $" Requirement -> hold {BigNumber.FormatUint(pair.Value.Requirement, group.TokenDecimal)}";
                    var mention = Context.Guild.GetRole(ulong.Parse(pair.Key)).Mention;
                    embed.AddField(str + inStr, $"{mention} | type `{Bot.CommandPrefix}claim {pair.Value.ClaimName}` to claim");
                }
                if (group.RoleDict.Count == 0)
                    embed.AddField($"{i++}. {group.GroupName} group:", $"none");
            }
            await ReplyAsync(embed: embed.Build());
        }

        [Command("claimall", RunMode = RunMode.Async)]
        public async Task ClaimAll() {
            if (Context.Guild == null) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            var tokenRoles = await RoleHandler.GetAllRolesByGuild(Context.Guild.Id);
            foreach (var role in tokenRoles) {
                var give = false;
                foreach (var add in addresses)
                    if (await Blockchain.ChainWatcher.GetBalanceOf(role.TokenAddress, add) >= BigInteger.Parse(role.GetBN())) {
                        give = true;
                        break;
                    }
                if (give) {
                    var user = Context.Message.Author as SocketGuildUser;
                    await user.AddRoleAsync(Context.Guild.GetRole(role.RoleId));
                }
            }
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
            //var groupRoles = await GroupHandler.GetAllGroupHandlerFromGuild(Context.Guild.Id);

            //foreach (var role in groupRoles) {
            //    var balance = BigInteger.Zero;
            //    var usedBalance = BigInteger.Zero;
            //    var role
            //    foreach (var add in addresses)
            //        balance += await Blockchain.ChainWatcher.GetBalanceOf(role.TokenAddress, add);
            //}
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("testfor", RunMode = RunMode.Async)]
        public async Task CheckGroupRoles(ulong user) {
            if (!await IsAdmin())
                return;
            //var (pair, group) = await GroupHandler.GetGroupRoleFromClaimName(claim, Context.Guild.Id);
            var group = await GroupHandler.GetGroupHandler(Context.Guild.Id, "whalegroup");
            await group.CheckOne(user);

        }

        [Command("AddroleFor", RunMode = RunMode.Async)]
        public async Task AddRoleFor(IUser user, string claim) {
            if (!await IsAdmin())
                return;
            var (pair, group) = await GroupHandler.GetGroupRoleFromClaimName(claim, Context.Guild.Id);
            if (group != null) {
                await ClaimGroupRoleFor(pair, group, user);
                return;
            }
        }

        [Command("claim", RunMode = RunMode.Async)]
        public async Task ClaimeRole(string claim)
        {
            var (pair, group) = await GroupHandler.GetGroupRoleFromClaimName(claim, Context.Guild.Id);
            if (group != null) {
                await ClaimGroupRole(pair, group);
                return;
            }
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
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            var give = false;
            foreach (var add in addresses)
                if (await Blockchain.ChainWatcher.GetBalanceOf(role.TokenAddress, add) >= BigInteger.Parse(role.GetBN()))
                {
                    give = true;
                    break;
                }
            if (give) {
                var user = Context.Message.Author as SocketGuildUser;
                await user.AddRoleAsync(Context.Guild.GetRole(role.RoleId));
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        [Command("testClaimFor", RunMode = RunMode.Async)]
        public async Task TestClaimRole(ulong id, string claim) {
            var user = await Context.Guild.GetUserAsync(id);
            var dUser = await Context.Guild.GetUserAsync(id);
            var (pair, group) = await GroupHandler.GetGroupRoleFromClaimName(claim, Context.Guild.Id);
            if (group != null) {
                await TestClaimGroupRole(pair, group, dUser);
                return;
            }
        }

        //public async Task TestClaimNFTRole(NFTRoleHandler role, IGuildUser user) {
        //    if (Context.Guild == null || Context.Guild.Id != role.guildId) {
        //        await ReplyAsync("Please use command in the correct server.");
        //        return;
        //    }
        //    var addresses = await User.GetUserAddresses(user.Id);
        //    if (addresses.Count == 0) {
        //        await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
        //        return;
        //    }
        //    await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
        //    var eligible = false;
        //    foreach (var add in addresses) {
        //        switch (role.RequirementType) {
        //            case NFTReqType.HoldX:
        //                eligible = await Blockchain.ChainWatcher.GetBalanceOf(role.NFTAddress, add) >= role.HoldXValue;
        //                break;
        //            case NFTReqType.InRange:
        //                eligible = (await Blockchain.OpenSea.CheckNFTInRange(add, role.NFTAddress, role.MinRange, role.MaxRange, role.HoldXValue));
        //                break;
        //            case NFTReqType.Custom:
        //                break;
        //        }
        //        if (eligible)
        //            break;
        //    }
        //    await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
        //    if (eligible) {
        //        //var user = Context.Message.Author as SocketGuildUser;
        //        var aRole = Context.Guild.GetRole(role.RoleId);
        //        try {
        //            //await user.AddRoleAsync(aRole);
        //        }
        //        catch (Exception e) { Console.WriteLine(e.Message); }
        //        await Context.Message.AddReactionAsync(new Emoji("✅"));
        //    }
        //    else
        //        await Context.Message.AddReactionAsync(new Emoji("❌"));
        //}


        public async Task ClaimNFTRole(NFTRoleHandler role) {
            if (Context.Guild == null || Context.Guild.Id != role.guildId) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
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
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            if (eligible) {
                var user = Context.Message.Author as SocketGuildUser;
                var aRole = Context.Guild.GetRole(role.RoleId);
                try {
                    await user.AddRoleAsync(aRole);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        [Command("remove", RunMode = RunMode.Async)]
        public async Task RemoveRoleUser(string claim) {
            if (Context.Guild == null)
                return;
            var user = Context.Message.Author as IGuildUser;
            var roleIds = user.RoleIds;
            var (pair, group) = await GroupHandler.GetGroupRoleFromClaimName(claim, Context.Guild.Id);
            if (group != null) {
                var role = Context.Guild.GetRole(ulong.Parse(pair.Key));
                if (roleIds.Contains(ulong.Parse(pair.Key))) {
                    await user.RemoveRoleAsync(role);
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                return;
            }
            var nftRole = await NFTRoleHandler.GetRoleByClaimName(claim, Context.Guild.Id);
            if (nftRole != null) {
                var role = Context.Guild.GetRole(nftRole.RoleId);
                if (roleIds.Contains(nftRole.RoleId)) {
                    await user.RemoveRoleAsync(role);
                    await Context.Message.AddReactionAsync(new Emoji("✅"));
                }
                return;
            }
            var tokenRole = await RoleHandler.GetRoleByClaimName(claim);
            if (tokenRole != null) {
                if (Context.Guild.Id == tokenRole.guildId) {
                    var role = Context.Guild.GetRole(tokenRole.RoleId);
                    if (roleIds.Contains(tokenRole.RoleId)) {
                        await user.RemoveRoleAsync(role);
                        await Context.Message.AddReactionAsync(new Emoji("✅"));
                    }
                    return;
                }
            }
        }

        [Command("removerole", RunMode = RunMode.Async)]
        public async Task AdminRemoveRole(ulong userId, ulong roleId) {
            if (!await IsAdmin())
                return;
            var user = Bot.GetUser(userId) as IGuildUser;
            var role = Context.Guild.GetRole(roleId);
            await user.RemoveRoleAsync(role);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        public async Task TestClaimGroupRole(KeyValuePair<string, GroupRole> pair, GroupHandler group, IGuildUser dUser) {
            if (Context.Guild == null || Context.Guild.Id != group.guildId) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var addresses = await User.GetUserAddresses(dUser.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            var balance = BigInteger.Zero;
            foreach (var address in addresses)
                balance += await Blockchain.ChainWatcher.GetBalanceOf(group.TokenAddress, address);
            var usedBalance = BigInteger.Zero;
            var roleReq = BigInteger.Parse(pair.Value.Requirement);
            var guildUser = dUser;
            var userRoles = guildUser.RoleIds;
            foreach (var role in group.RoleDict) {
                if (userRoles.Contains(ulong.Parse(role.Key)))
                    usedBalance += BigInteger.Parse(role.Value.Requirement);
            }
            var eligible = balance >= usedBalance + roleReq;
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            if (eligible) {
                var user = dUser as SocketGuildUser;
                var aRole = Context.Guild.GetRole(ulong.Parse(pair.Key));
                try {
                    await user.AddRoleAsync(aRole);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        public async Task ClaimGroupRole(KeyValuePair<string, GroupRole> pair, GroupHandler group) {
            if (Context.Guild == null || Context.Guild.Id != group.guildId) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            var balance = BigInteger.Zero;
            foreach (var address in addresses)
                balance += await Blockchain.ChainWatcher.GetBalanceOf(group.TokenAddress, address);
            var usedBalance = BigInteger.Zero;
            var roleReq = BigInteger.Parse(pair.Value.Requirement);
            var guildUser = Context.Message.Author as IGuildUser;
            var userRoles = guildUser.RoleIds;
            foreach (var role in group.RoleDict) {
                if (userRoles.Contains(ulong.Parse(role.Key)))
                    usedBalance += BigInteger.Parse(role.Value.Requirement);
            }
            var eligible = balance >= usedBalance + roleReq;
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            if (eligible) {
                var user = Context.Message.Author as SocketGuildUser;
                var aRole = Context.Guild.GetRole(ulong.Parse(pair.Key));
                try {
                    await user.AddRoleAsync(aRole);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }

        public async Task ClaimGroupRoleFor(KeyValuePair<string, GroupRole> pair, GroupHandler group, IUser user) {
            if (Context.Guild == null || Context.Guild.Id != group.guildId) {
                await ReplyAsync("Please use command in the correct server.");
                return;
            }
            var addresses = await User.GetUserAddresses(user.Id);
            if (addresses.Count == 0) {
                await ReplyAsync($"User has not binded an address. Please Bind an address using command `{Bot.CommandPrefix}verify`");
                return;
            }
            await Context.Message.AddReactionAsync(Emote.Parse("<a:loading:726356725648719894>"));
            var balance = BigInteger.Zero;
            foreach (var address in addresses)
                balance += await Blockchain.ChainWatcher.GetBalanceOf(group.TokenAddress, address);
            var usedBalance = BigInteger.Zero;
            var roleReq = BigInteger.Parse(pair.Value.Requirement);
            var guildUser = user as IGuildUser;
            var userRoles = guildUser.RoleIds;
            foreach (var role in group.RoleDict) {
                if (userRoles.Contains(ulong.Parse(role.Key)))
                    usedBalance += BigInteger.Parse(role.Value.Requirement);
            }
            var eligible = balance >= usedBalance + roleReq;
            await Context.Message.RemoveReactionAsync(Emote.Parse("<a:loading:726356725648719894>"), Context.Client.CurrentUser.Id);
            if (eligible) {
                var roleUser = user as SocketGuildUser;
                var aRole = Context.Guild.GetRole(ulong.Parse(pair.Key));
                try {
                    await roleUser.AddRoleAsync(aRole);
                }
                catch (Exception e) { Console.WriteLine(e.Message); }
                await Context.Message.AddReactionAsync(new Emoji("✅"));
            }
            else
                await Context.Message.AddReactionAsync(new Emoji("❌"));
        }


        [Command("unlink", RunMode = RunMode.Async)]
        public async Task UnlinkWallet() {
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync("User has not binded an address.");
                return;
            }
            await User.DeleteUser(Context.Message.Author.Id);
        }

        [Command("address", RunMode = RunMode.Async)]
        public async Task ShowAddresses() {
            var addresses = await User.GetUserAddresses(Context.Message.Author.Id);
            if (addresses.Count == 0) {
                await ReplyAsync("User has not binded an address.");
                return;
            }
            var str = "";
            foreach (var add in addresses)
                str += $"- {add}\n";
            await Context.Message.Author.SendMessageAsync(str);
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
