using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;

namespace RoleHandlerBot
{
    public class Bot
    {
        public static char CommandPrefix;
        public static DiscordSocketClient DiscordClient { get; set; }
        private readonly CommandService _commands;

        /// <summary>
        /// Constructor for the Bot class.
        /// </summary>
        public Bot()
        {
            if (DiscordClient != null)
            {
                throw new Exception("Bot already running");
            }
            Logger.LogInternal("Creating client.");
            DiscordClient = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 1000,

                // If your platform doesn't have native websockets,
                // add Discord.Net.Providers.WS4Net from NuGet,
                // add the `using` at the top, and uncomment this line:
                //WebSocketProvider = WS4NetProvider.Instance
            });

            _commands = new CommandService();
            DiscordClient.Log += Logger.Log;
            DiscordClient.MessageReceived += HandleCommandAsync;
            if (Program.IsRelease)
                DiscordClient.Ready += RoleHandler.RunChecks;
        }

        ~Bot()
        {
            DiscordClient = null;
        }

        private string GetUserName(SocketUser socketUser)
        {
            string userName = "NULL";
            try
            {
                if (socketUser != null)
                {
                    userName = socketUser.ToString();
                    SocketGuildUser user = socketUser as SocketGuildUser;
                    if (user?.Nickname != null)
                    {
                        userName += " NickName: " + user.Nickname;
                    }
                }

            }
            catch { }
            return userName;
        }

        public static IUser GetUser(ulong userId) => DiscordClient.GetUser(userId);

        public static SocketChannel GetChannelContext(ulong channelId) => DiscordClient.GetChannel(channelId);


        public async Task HandleCommandAsync(SocketMessage messageParam)
        {
            string userName = "";
            string channelName = "";
            string guildName = "";
            try
            {
                var msg = messageParam as SocketUserMessage;
                if (msg == null) return;

                var context = new CommandContext(DiscordClient, msg);
                guildName = context.Guild?.Name ?? "NULL";
                int argPos = 0;
                if (msg.HasCharPrefix(CommandPrefix, ref argPos))
                {
                    userName = GetUserName(msg.Author);
                    channelName = msg.Channel?.Name ?? "NULL";
                    guildName = context.Guild?.Name ?? "NULL";
                    Logger.LogInternal($"HandleCommandAsync G: {guildName} C: {channelName} User: {userName}  Msg: {msg}");

                    var result = await _commands.ExecuteAsync(context, argPos, null);

                    if (!result.IsSuccess) // If execution failed, reply with the error message.
                    {
                        string message = "Command Failed: " + result;
                        await Logger.Log(new LogMessage(LogSeverity.Error, "HandleCommandAsync", message));
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    await Logger.Log(new LogMessage(LogSeverity.Error, "HandleCommandAsync", $"G:{guildName} C:{channelName} U:{userName} Unexpected Exception", ex));
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Start the Discord client.
        /// </summary>
        public async Task RunAsync(string token, string prefix)
        {
            Logger.LogInternal("Registering commands.");
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);

            Logger.LogInternal($"Setting prefix to: {prefix}");
            CommandPrefix = char.Parse(prefix);

            Logger.LogInternal("Connecting to the server.");
            await DiscordClient.LoginAsync(TokenType.Bot, token);
            await DiscordClient.StartAsync();
            //await DiscordClient.LogoutAsync();
            await Task.Delay(-1); ;
        }
    }
}