using DSharpPlus.Entities;

namespace DisControl; 

public static class AutoReset {
    public static void Start()
        => Task.Run(Runner);

    private static async Task Runner() {
        while (true) {
            if (!Configuration.Instance.Other.AutoRestart) {
                await Task.Delay(5000);
                continue;
            }
            var start = DateTime.Now;
            var end = start + TimeSpan.FromHours((double)Configuration.Instance.Other.HoursUntilReset!);
            while (DateTime.Now < end) {
                if (!Configuration.Instance.Other.AutoRestart) break;
                end = start + TimeSpan.FromHours((double)Configuration.Instance.Other.HoursUntilReset!);
                await Task.Delay(5000); 
            }

            if (!Configuration.Instance.Other.AutoRestart) continue;
            await Qemu.ResetFrom(Configuration.Instance.Other.ResetSource!,
                Configuration.Instance.Other.ResetTarget!);
            await Qemu.StartVM(Configuration.Instance.Other.ResetTarget!);
            await DiscordLog.Send(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Cyan)
                .WithTitle("Virtual machine has been automatically reset")
                .AddField("Display Name", Qemu.CurrentVM!.DisplayName)
                .AddField("Identifier", Qemu.CurrentVM.Identifier)
                .Build());
        }
    }
}