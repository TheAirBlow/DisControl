using DSharpPlus.SlashCommands;

namespace DisControl.Attributes; 

public class AdminOnlyCommandAttribute : SlashCheckBaseAttribute {
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(Configuration.Instance.Discord.Admins.Contains(ctx.Member.Id));
}