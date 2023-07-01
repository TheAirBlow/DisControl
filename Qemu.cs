using System.Diagnostics;
using DSharpPlus.Entities;
using Serilog;

namespace DisControl; 

public static class Qemu {
    public static Configuration.VirtualMachine? CurrentVM { get; private set; }
    public static DateTime StartTime { get; private set; }
    private static CancellationTokenSource _stopToken = new();

    static Qemu() {
        if (!Directory.Exists("vms"))
            Directory.CreateDirectory("vms");
    }

    public static async Task<String> Clone(string id, string name) {
        var source = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == id);
        if (source == null) throw new InvalidOperationException(
            "Invalid identifier specified");
        var vm = new Configuration.VirtualMachine {
            Memory = source.Memory,
            DisplayName = name
        };

        Directory.CreateDirectory(Path.Combine("vms", vm.Identifier));
        var original = Path.Combine("vms", source.Identifier);
        var clone = Path.Combine("vms", vm.Identifier);
        File.Copy(Path.Combine(original, "drive.img"), 
            Path.Combine(clone, "drive.img"));
        File.Copy(Path.Combine(original, "cdrom.img"), 
            Path.Combine(clone, "cdrom.img"));
        Configuration.Instance.VirtualMachines.Add(vm);
        Configuration.SaveChanges();
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Cyan)
            .WithTitle("A virtual machine has been cloned")
            .AddField("Display Name", vm.DisplayName)
            .AddField("Identifier", vm.Identifier)
            .Build());
        return vm.Identifier;
    }

    public static async Task<String> Create(string name,
        string install, long memory, long driveSize) {
        var vm = new Configuration.VirtualMachine {
            DisplayName = name,
            Memory = memory
        };

        if (!File.Exists(install)) throw new InvalidOperationException(
            "The installation disk file specified does not exist");
        Directory.CreateDirectory(Path.Combine("vms", vm.Identifier));
        var drive = Path.Combine(Environment.CurrentDirectory, "vms", vm.Identifier, "drive.img");
        var cdrom = Path.Combine(Environment.CurrentDirectory, "vms", vm.Identifier, "cdrom.img");
        var info = new ProcessStartInfo {
            FileName = "qemu-img",
            Arguments = $"create -f qcow2 \"{drive}\" {driveSize}G",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        await Process.Start(info)!.WaitForExitAsync();
        File.Copy(install, cdrom);
        Configuration.Instance.VirtualMachines.Add(vm);
        Configuration.SaveChanges();
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("New virtual machine has been created")
            .AddField("Display Name", vm.DisplayName)
            .AddField("Identifier", vm.Identifier)
            .Build());
        return vm.Identifier;
    }

    public static async Task Delete(string id) {
        var source = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == id);
        if (source == null) throw new InvalidOperationException(
            "Invalid identifier specified");
        
        if (CurrentVM?.Identifier == id) await StopVM();
        Directory.Delete(Path.Combine("vms", source.Identifier), true);
        Configuration.Instance.VirtualMachines.Remove(source);
        Configuration.SaveChanges();
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTitle("A virtual machine has been deleted")
            .AddField("Display Name", source.DisplayName)
            .AddField("Identifier", source.Identifier)
            .Build());
    }

    public static async Task ResetFrom(string sourceId, string targetId) {
        var source = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == sourceId);
        if (source == null) throw new InvalidOperationException(
            "Invalid source identifier specified");
        var target = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == targetId);
        if (target == null) throw new InvalidOperationException(
            "Invalid target identifier specified");
        
        if (CurrentVM?.Identifier == targetId) await StopVM();
        var original = Path.Combine("vms", source.Identifier);
        var clone = Path.Combine("vms", target.Identifier);
        File.Copy(Path.Combine(original, "drive.img"), 
            Path.Combine(clone, "drive.img"));
        File.Copy(Path.Combine(original, "cdrom.img"), 
            Path.Combine(clone, "cdrom.img"));
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Yellow)
            .WithTitle($"{target.DisplayName} has been reset")
            .AddField("Backup Name", source.DisplayName)
            .AddField("Backup Identifier", source.Identifier)
            .AddField("Target Identifier", target.Identifier)
            .Build());
    }

    public static async Task StartVM(string id) {
        if (CurrentVM != null) throw new InvalidOperationException(
            "A virtual machine is already running");
        var source = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == id);
        CurrentVM = source ?? throw new InvalidOperationException(
            "Invalid identifier specified");
        StartTime = DateTime.Now;
        _ = Task.Run(Runner);
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("A virtual machine has been started")
            .AddField("Display Name", source.DisplayName)
            .AddField("Identifier", source.Identifier)
            .Build());
    }

    public static async Task SwitchTo(string id) {
        var source = Configuration.Instance.VirtualMachines.FirstOrDefault(
            x => x.Identifier == id);
        if (source == null) throw new InvalidOperationException(
            "Invalid identifier specified");
        if (CurrentVM != null) await StopVM();
        StartTime = DateTime.Now;
        CurrentVM = source;
        _ = Task.Run(Runner);
        
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Switched virtual machine")
            .AddField("Display Name", source.DisplayName)
            .AddField("Identifier", source.Identifier)
            .Build());
    }
    
    public static async Task StopVM() {
        if (CurrentVM == null) throw new InvalidOperationException(
            "No virtual machine is running right now");
        var vm = CurrentVM;
        _stopToken.Cancel();
        _stopToken = new CancellationTokenSource();
        await DiscordLog.Send(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Red)
            .WithTitle("A virtual machine has been stopped")
            .AddField("Display Name", vm.DisplayName)
            .AddField("Identifier", vm.Identifier)
            .Build());
    }
    
    private static async Task Runner() {
        Log.Information($"[Runner] Starting {CurrentVM!.Identifier}");
        try {
            var drive = Path.Combine(Environment.CurrentDirectory, "vms", CurrentVM.Identifier, "drive.img");
            var cdrom = Path.Combine(Environment.CurrentDirectory, "vms", CurrentVM.Identifier, "cdrom.img");
            var info = new ProcessStartInfo {
                FileName = "qemu-system-x86_64",
                Arguments = $"-m {CurrentVM.Memory} -hda \"{drive}\" -cdrom \"{cdrom}\" -vnc 127.0.0.1:1 -nographic",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var proc = Process.Start(info);
            try {
                await proc!.WaitForExitAsync(_stopToken.Token);
            } catch { /* Ignore */ }
            if (!proc!.HasExited) {
                Log.Information($"[Runner] Killing virtual machine");
                proc.Kill(); 
            }
            if (!Configuration.Instance.Other.AutoRestart) CurrentVM = null;
        } catch (Exception e) {
            Log.Error("Exception in runner: {0}", e);
        }
        
        if (Configuration.Instance.Other.AutoRestart) {
            Log.Information("[Runner] Auto-restart enabled, restarting virtual machine");
            StartTime = DateTime.Now; _ = Task.Run(Runner);
            await DiscordLog.Send(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Cyan)
                .WithTitle("Virtual machine has been automatically restarted")
                .AddField("Display Name", CurrentVM!.DisplayName)
                .AddField("Identifier", CurrentVM!.Identifier)
                .Build());
            return;
        } 
        
        Log.Information("[Runner] Task finished, virtual machine stopped");
    }
}