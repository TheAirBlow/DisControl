using DisControl;
using DisControl.Attributes;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Serilog;
using Configuration = DisControl.Configuration;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("[DisControl] Starting automatic reset task");
AutoReset.Start();
    
Log.Information("[DisControl] Initializing Discord");
var factory = new LoggerFactory().AddSerilog();
var client = new DiscordClient(new DiscordConfiguration {
    Token = Configuration.Instance.Discord.BotToken,
    Intents = DiscordIntents.AllUnprivileged,
    TokenType = TokenType.Bot,
    LoggerFactory = factory
});

Log.Information("[DisControl] Setting up slash commands");
var slash = client.UseSlashCommands();
slash.RegisterCommands<Commands>();
slash.SlashCommandErrored += async (s, e) => {
    if (e.Exception is not SlashExecutionChecksFailedException slex) return;
    foreach (var check in slex.FailedChecks)
        switch (check) {
            case CommandChannelAttribute _:
                await e.Context.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Channels exist for a reason")
                    .WithDescription("This is not a place for DisControl commands")
                    .Build(), true);
                return;
            case AdminOnlyCommandAttribute _:
                await e.Context.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Laugh at this silly person")
                    .WithDescription("He tried to run an admin command, haha!")
                    .Build());
                return;
            case MachineMustBeOnAttribute _:
                await e.Context.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Virtual machine is not running")
                    .WithDescription("Wait for an admin to start one")
                    .Build());
                return;
            case VncCommandAttribute _:
                await e.Context.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Unable to connect to VNC")
                    .WithDescription("This is really weird, error logged")
                    .Build());
                return;
        }
};


Log.Information("[DisControl] Connecting to Discord");
await client.ConnectAsync();
client.Ready += async (_, _) => {
    await client.UpdateStatusAsync(new DiscordActivity(
        "with VMWare", ActivityType.Playing));
};

Log.Information("[DisControl] Loading log channels");
await DiscordLog.Load(client);
await DiscordLog.Send(new DiscordEmbedBuilder()
    .WithColor(DiscordColor.Aquamarine)
    .WithTitle("Ready for action")
    .WithDescription("I'm now a slave of this discord server, waah")
    .Build());

Log.Information("[DisControl] Initialization finished successfully");
await Task.Delay(-1);