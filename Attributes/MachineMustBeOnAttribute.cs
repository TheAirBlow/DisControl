using DSharpPlus.SlashCommands;

namespace DisControl.Attributes; 

public class MachineMustBeOnAttribute : SlashCheckBaseAttribute {
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        => Task.FromResult(Qemu.CurrentVM != null);
}