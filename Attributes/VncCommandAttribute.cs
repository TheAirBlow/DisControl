using DSharpPlus.SlashCommands;
using Serilog;

namespace DisControl.Attributes; 

public class VncCommandAttribute : SlashCheckBaseAttribute {
    public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx) {
        try {
            if (!VncManager.IsConnected())
                await VncManager.Connect();
            return true;
        } catch (Exception e) {
            Log.Error("Exception: {0}", e);
            return false;
        }
    }
}