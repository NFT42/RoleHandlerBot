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
        public bool IsAdmin()
        {
            var user = Context.Message.Author as SocketGuildUser;
            if (user.Id == 195567858133106697)
                return true;
            var roles = user.Roles;
            foreach (var role in roles)
                if (role.Name == "Avastars Corp" || role.Name.Contains("Admin"))
                    return true;
            return false;
        }

        [Command("AddRole", RunMode = RunMode.Async)]
        public async Task AddRole(IRole role, string tokenName, string token, int req, int dec, string name)
        {
            if (!IsAdmin())
                return;
            if (Context.Guild == null)
            {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await RoleHandler.AddRoleHandler(Context.Guild.Id, role.Id, token, req, dec, name, tokenName);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("DeleteRole", RunMode = RunMode.Async)]
        public async Task Dlete(IRole role)
        {
            if (!IsAdmin())
                return;
            if (Context.Guild == null)
            {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            await RoleHandler.RemoveRoleHandler(role.Id);
            await Context.Message.AddReactionAsync(new Emoji("✅"));
        }

        [Command("TestRun", RunMode = RunMode.Async)]
        public async Task TestRun() {

            await RoleHandler.CheckAllRolesReq();
        }

        [Command("showRoles", RunMode = RunMode.Async)]
        public async Task ShowRoles()
        {
            if (!IsAdmin())
                return;
            if (Context.Guild == null)
            {
                await ReplyAsync("You must issue this command inside a server!");
                return;
            }
            var roles = await RoleHandler.GetAllRoles();
            roles = roles.Where(r => r.guildId == Context.Guild.Id).ToList();
            var embed = new EmbedBuilder().WithTitle("Roles").WithColor(Color.Blue);
            embed.WithDescription("Delete a role handler using `$deleteRole @role`");

            int i = 1;
            foreach (var role in roles) {
                var mention = Context.Guild.GetRole(role.RoleId).Mention;
                embed.AddField($"{i}. Requirement: {role.Requirement} {role.TokenName}", $"{mention}");
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
                await ReplyAsync("User has not binded an address. Please Bind an address using command `$verify your_address` example `$verify 0x123456789abcdefABCDEF9876543210123456789`");
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
                var embed = new EmbedBuilder().WithTitle("Follow this link to verify your address");
                embed.WithColor(Color.DarkMagenta);
                embed.WithUrl("https://cesarsld.github.io/AvastarVerifyPage/?" + $"discordId={Context.Message.Author.Id}&address={address}");
                await ReplyAsync(embed: embed.Build());
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
