using DSharpPlus.SlashCommands;
using Serilog;

namespace DisControl.Attributes; 

public class CommandChannelAttribute : SlashCheckBaseAttribute {
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(Configuration.Instance.Discord.CommandChannels.Any(
            x => x.Channel == ctx.Channel.Id && x.Guild == ctx.Guild.Id));
}