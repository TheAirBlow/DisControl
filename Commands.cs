using DisControl.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using MarcusW.VncClient;

namespace DisControl; 

public class Commands : ApplicationCommandModule {
    public enum Options { 
        HoursUntilReset,
        PeriodicReset, 
        ResetSource,
        ResetTarget,
        AutoRestart
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("configure", "Sets a configuration option (admin-only)")]
    public async Task Configure(InteractionContext ctx,
        [Option("option", "Option to change")] Options option,
        [Option("boolValue", "Boolean value")] bool? boolValue = null,
        [Option("intValue", "Integer value")] long? intValue = null,
        [Option("stringValue", "String value")] string? stringValue = null) {
        try {
            switch (option) {
                case Options.HoursUntilReset:
                    if (!intValue.HasValue) throw new Exception(
                        $"{option} requires an integer value");
                    Configuration.Instance.Other.HoursUntilReset = (int)intValue;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle($"Set {option} successfully")
                            .WithDescription($"New value: {intValue}")
                            .Build()));
                    break;
                case Options.PeriodicReset:
                    if (!boolValue.HasValue) throw new Exception(
                        $"{option} requires an boolean value");
                    Configuration.Instance.Other.PeriodicReset = boolValue.Value;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle($"Set {option} successfully")
                            .WithDescription($"New value: {boolValue}")
                            .Build()));
                    break;
                case Options.ResetSource:
                    if (stringValue == null) throw new Exception(
                        $"{option} requires a string value");
                    Configuration.Instance.Other.ResetSource = stringValue;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle($"Set {option} successfully")
                            .WithDescription($"New value: {stringValue}")
                            .Build()));
                    break;
                case Options.ResetTarget:
                    if (stringValue == null) throw new Exception(
                        $"{option} requires a string value");
                    Configuration.Instance.Other.ResetTarget = stringValue;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle($"Set {option} successfully")
                            .WithDescription($"New value: {stringValue}")
                            .Build()));
                    break;
                case Options.AutoRestart:
                    if (!boolValue.HasValue) throw new Exception(
                        $"{option} requires an boolean value");
                    Configuration.Instance.Other.AutoRestart = boolValue.Value;
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle($"Set {option} successfully")
                            .WithDescription($"New value: {boolValue}")
                            .Build()));
                    break;
            }
            
            Configuration.SaveChanges();
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to set an option")
                    .WithDescription(e.Message)
                    .Build()));
        }
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("create", "Creates a new virtual machine from scratch (admin-only)")]
    public async Task CreateVM(InteractionContext ctx,
        [Option("name", "Display name of the virtual machine")] string name,
        [Option("install", "Installation disk file path")] string install,
        [Option("memory", "RAM allocation for the virtual machine (in MB)")] long memory,
        [Option("driveSize", "Size of the main drive (in GB)")] long driveSize) {
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Yellow)
            .WithTitle("Creating a new virtual machine")
            .WithDescription("Please give me a moment, working hard...")
            .Build());
        try {
            var id = Qemu.Create(name, install, memory, driveSize);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithTitle("Created a new virtual machine successfully")
                    .WithDescription($"Identifier: {id}")
                    .Build()));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to create a new virtual machine")
                    .WithDescription(e.Message)
                    .Build()));
        }
    }

    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("clone", "Clones a virtual machine (admin-only)")]
    public async Task CloneVM(InteractionContext ctx,
        [Option("source", "Identifier of the virtual machine to clone")] string source,
        [Option("name", "Display name of the virtual machine")] string name) {
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Yellow)
            .WithTitle("Cloning a virtual machine")
            .WithDescription("Please give me a moment, working hard...")
            .Build());
        try {
            var id = Qemu.Clone(source, name);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithTitle("Cloned a virtual machine successfully")
                    .WithDescription($"Identifier: {id}")
                    .Build()));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to clone a virtual machine")
                    .WithDescription(e.Message)
                    .Build()));
        }
    }
    
    [AdminOnlyCommand]
    [SlashCommand("setCommandChannel", "Makes current channel a command channel (admin-only)")]
    public async Task SetCommandChannel(InteractionContext ctx) {
        var channel = new Configuration.DiscordClass.GuildChannel {
            Channel = ctx.Channel.Id,
            Guild = ctx.Guild.Id
        };
        
        if (Configuration.Instance.Discord.CommandChannels.Contains(channel)) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed at making current channel a command channel")
                    .WithDescription("This channel is already used for commands, duplicate!")
                    .Build()));
            return;
        }
        
        Configuration.Instance.Discord.CommandChannels.Add(channel);
        Configuration.SaveChanges();
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Made current channel a command channel")
            .WithDescription("Operation successfully completed!")
            .Build());
    }
    
    [AdminOnlyCommand]
    [SlashCommand("unsetCommandChannel", "Makes current channel no longer used for commands (admin-only)")]
    public async Task UnsetCommandChannel(InteractionContext ctx) {
        var channel = new Configuration.DiscordClass.GuildChannel {
            Channel = ctx.Channel.Id,
            Guild = ctx.Guild.Id
        };
        
        if (!Configuration.Instance.Discord.CommandChannels.Contains(channel)) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to make current channel no longer used for commands")
                    .WithDescription("This channel is not used for commands, nothing to delete!")
                    .Build()));
            return;
        }
        
        Configuration.Instance.Discord.CommandChannels.Remove(channel);
        Configuration.SaveChanges();
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Made current channel no longer used for commands")
            .WithDescription("Operation successfully completed!")
            .Build());
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("setLogChannel", "Makes current channel a log channel (admin-only)")]
    public async Task SetLogChannel(InteractionContext ctx) {
        var channel = new Configuration.DiscordClass.GuildChannel {
            Channel = ctx.Channel.Id,
            Guild = ctx.Guild.Id
        };
        
        if (Configuration.Instance.Discord.LogChannels.Contains(channel)) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to make current channel a log channel")
                    .WithDescription("This channel is already used for logging, duplicate!")
                    .Build()));
            return;
        }
        
        Configuration.Instance.Discord.LogChannels.Add(channel);
        Configuration.SaveChanges();
        await DiscordLog.Load();
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Made current channel a log channel")
            .WithDescription("Operation successfully completed!")
            .Build());
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("unsetLogChannel", "Makes current channel no longer used for logging (admin-only)")]
    public async Task UnsetLogChannel(InteractionContext ctx) {
        var channel = new Configuration.DiscordClass.GuildChannel {
            Channel = ctx.Channel.Id,
            Guild = ctx.Guild.Id
        };
        
        if (!Configuration.Instance.Discord.LogChannels.Contains(channel)) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to make current channel no longer used for logging")
                    .WithDescription("This channel is not used for logging, nothing to delete!")
                    .Build()));
            return;
        }
        
        Configuration.Instance.Discord.LogChannels.Remove(channel);
        Configuration.SaveChanges();
        await DiscordLog.Load();
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Made current channel no longer used for logging")
            .WithDescription("Operation successfully completed!")
            .Build());
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("delete", "Deletes a virtual machine (admin-only)")]
    public async Task DeleteVM(InteractionContext ctx, 
        [Option("id", "Identifier of the virtual machine")] string id) {
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Yellow)
            .WithTitle("Deleting the current or parent VM")
            .WithDescription("Please give me a moment, working hard...")
            .Build());
        try {
            await Qemu.Delete(id);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithTitle("Deleted a virtual machine successfully")
                    .WithDescription($"Identifier: {id}")
                    .Build()));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to delete a virtual machine")
                    .WithDescription(e.Message)
                    .Build()));
        }
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("resetFrom", "Resets a virtual machine by copying a backup virtual machine (admin-only)")]
    public async Task ResetFrom(InteractionContext ctx, 
        [Option("sourceId", "Identifier of the source virtual machine")] string sourceId,
        [Option("targetId", "Identifier of the target virtual machine")] string targetId) {
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Yellow)
            .WithTitle("Resetting a virtual machine")
            .WithDescription("Please give me a moment, working hard...")
            .Build());
        try {
            await Qemu.ResetFrom(sourceId, targetId);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Green)
                    .WithTitle("Reset a virtual machine successfully")
                    .AddField("Source Identifier", sourceId)
                    .AddField("Target Identifier", targetId)
                    .Build()));
        } catch (Exception e) {
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Failed to reset a virtual machine")
                    .WithDescription(e.Message)
                    .Build()));
        }
    }

    public enum PowerAction {
        Start, Stop
    }
    
    [AdminOnlyCommand] [CommandChannel]
    [SlashCommand("powerAction", "Performs a power action on a virtual machine (admin-only)")]
    public async Task SetState(InteractionContext ctx,
        [Option("action", "Action to perform")] PowerAction action,
        [Option("id", "Identifier of the virtual machine")] string? id = null) {
        id ??= Qemu.CurrentVM?.Identifier;
        try {
            if (id == null) throw new Exception("No VM is currently running, specify an identifier");
            if (Qemu.CurrentVM?.Identifier == id) {
                switch (action) {
                    case PowerAction.Start:
                        throw new Exception("This virtual machine is already running");
                    case PowerAction.Stop:
                        await Qemu.StopVM();
                        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle("Stopped a virtual machine successfully")
                            .WithDescription($"Identifier: {id}")
                            .Build());
                        break;
                }
            }

            if (Qemu.CurrentVM != null && Qemu.CurrentVM.Identifier != id) {
                switch (action) {
                    case PowerAction.Start:
                        var old = Qemu.CurrentVM.Identifier;
                        await Qemu.SwitchTo(id);
                        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle("Switched virtual machines successfully")
                            .AddField("Old Identifier", old)
                            .AddField("New Identifier", id)
                            .Build());
                        break;
                    case PowerAction.Stop:
                        throw new Exception("This virtual machine is not running");
                }
            }

            if (Qemu.CurrentVM == null) {
                switch (action) {
                    case PowerAction.Start:
                        await Qemu.StartVM(id);
                        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                            .WithColor(DiscordColor.Green)
                            .WithTitle("Started a virtual machine successfully")
                            .WithDescription($"Identifier: {id}")
                            .Build());
                        break;
                    case PowerAction.Stop:
                        throw new Exception("This virtual machine is not running");
                }
            }
        } catch (Exception e) {
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Failed to perform a power action")
                .WithDescription(e.Message)
                .Build());
        }
    }
    
    [CommandChannel]
    [SlashCommand("list", "Lists all VMs")]
    public async Task List(InteractionContext ctx) {
        var embed = new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle($"Listing {Configuration.Instance.VirtualMachines.Count} virtual machines");
        foreach (var i in Configuration.Instance.VirtualMachines)
            embed.AddField(i.DisplayName, i.Identifier);
        await ctx.CreateResponseAsync(embed.Build());
    }

    [CommandChannel]
    [SlashCommand("info", "Tells you some basic information")]
    public async Task Info(InteractionContext ctx) {
        await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
            .WithColor(DiscordColor.Green)
            .WithTitle("Information")
            .AddField("Running VM", Qemu.CurrentVM != null ? Qemu.CurrentVM.Identifier : "None")
            .AddField("Running since", Qemu.CurrentVM != null 
                ? $"<t:{((DateTimeOffset)Qemu.StartTime).ToUnixTimeSeconds()}:R>" : "Not running").Build());
    }

    public enum FastCombo {
        SelectAll, ClearTextBox, Copy, Cut, Paste
    }

    [MachineMustBeOn] [VncCommand] [CommandChannel]
    [SlashCommand("screen", "Show VM's screen")]
    public async Task Screen(InteractionContext ctx) {
        var stream = await VncManager.GetScreenPNG();
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddFile("screen.png", stream));
        await stream.DisposeAsync();
    }
    
    public enum MouseButton {
        None = 0,
        Left = 1,
        Middle = 2,
        Right = 4,
        WheelUp = 8,
        WheelDown = 16,
        WheelLeft = 32,
        WheelRight = 64
    }
    
    [MachineMustBeOn] [VncCommand] [CommandChannel]
    [SlashCommand("mouse", "Performs a mouse action")]
    public async Task Mouse(InteractionContext ctx, 
        [Option("button", "Mouse Button")] MouseButton button,
        [Option("x", "Mouse X")] long x, [Option("y", "Mouse Y")] long y,
        [Option("relative", "Mouse action relative to last mouse action")] bool relative = false,
        [Option("autoRelease", "Automatically release button")] bool autoRelease = false) {
        await VncManager.SendMouse((int) x, (int) y, (MouseButtons) button, relative);
        if (autoRelease) {
            if (relative) await VncManager.SendMouse(0, 0, MouseButtons.None, true);
            else await VncManager.SendMouse((int) x, (int) y, MouseButtons.None);
        }

        await Task.Delay(500);
        var stream = await VncManager.GetScreenPNG();
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddFile("screen.png", stream));
        await stream.DisposeAsync();
    }
    
    [MachineMustBeOn] [VncCommand] [CommandChannel]
    [SlashCommand("keyPress", "Presses and releases a key")]
    public async Task KeyPress(InteractionContext ctx, [Option("key", "Self-explanatory")] string key) {
        if (!Enum.TryParse(key, out KeySymbol keysym)) {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .AddEmbed(new DiscordEmbedBuilder()
                    .WithColor(DiscordColor.Red)
                    .WithTitle("Invalid KeySymbol")
                    .WithDescription("See reference link below")
                    .Build()).AddComponents(new DiscordLinkButtonComponent(
                    "https://t.ly/N4Hmh", "KeySymbol reference")));
            return;
        }
        
        await VncManager.SendKey(keysym);
        await Task.Delay(500);
        var stream = await VncManager.GetScreenPNG();
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddFile("screen.png", stream));
        await stream.DisposeAsync();
    }
    
    [MachineMustBeOn] [VncCommand] [CommandChannel]
    [SlashCommand("type", "Types text (set appropriate language on the VM first)")]
    public async Task Type(InteractionContext ctx, [Option("text", "Self-explanatory")] string text) {
        await VncManager.Type(text);
        await Task.Delay(500);
        var stream = await VncManager.GetScreenPNG();
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddFile("screen.png", stream));
        await stream.DisposeAsync();
    }

    [MachineMustBeOn] [VncCommand] [CommandChannel]
    [SlashCommand("keyCombo", "Perform a key combo")]
    public async Task KeyCombo(InteractionContext ctx, 
        [Option("customComboKey1", "Custom key combination, key #1")] string? customComboKey1 = null,
        [Option("customComboKey2", "Custom key combination, key #2")] string? customComboKey2 = null,
        [Option("customComboKey3", "Custom key combination, key #3")] string? customComboKey3 = null,
        [Option("customComboKey4", "Custom key combination, key #4")] string? customComboKey4 = null,
        [Option("customComboKey5", "Custom key combination, key #5")] string? customComboKey5 = null,
        [Option("fastCombo", "Use a key combination shortcut")] FastCombo? fastCombo = null) {
        if (customComboKey1 == null && fastCombo == null) {
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("No key combo specified")
                .WithDescription("What did you expect?")
                .Build(), true);
            return;
        }
        
        if (customComboKey1 != null && fastCombo != null) {
            await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Red)
                .WithTitle("Both custom and fast combo specified")
                .WithDescription("I can't do both at a time, sorry")
                .Build(), true);
            return;
        }

        if (fastCombo == null) {
            var list = new List<KeySymbol>();
            var failed = false;
            if (customComboKey1 != null)
                if (!Enum.TryParse(customComboKey1, out KeySymbol key))
                    failed = true; else list.Add(key);
            if (customComboKey2 != null)
                if (!Enum.TryParse(customComboKey1, out KeySymbol key))
                    failed = true; else list.Add(key);
            if (customComboKey3 != null)
                if (!Enum.TryParse(customComboKey1, out KeySymbol key))
                    failed = true; else list.Add(key);
            if (customComboKey4 != null)
                if (!Enum.TryParse(customComboKey1, out KeySymbol key))
                    failed = true; else list.Add(key);
            if (customComboKey5 != null)
                if (!Enum.TryParse(customComboKey1, out KeySymbol key))
                    failed = true; else list.Add(key);
            if (list.Count < 2) {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .AddEmbed(new DiscordEmbedBuilder()
                        .WithColor(DiscordColor.Red)
                        .WithTitle("This is not a key combo")
                        .WithDescription("Two combo keys should be set at minimum, " +
                                         "or the ones you set were invalid")
                        .Build()).AddComponents(new DiscordLinkButtonComponent(
                        "https://t.ly/N4Hmh", "KeySymbol reference")));
                return;
            }
            
            await VncManager.SendKeyCombo(list.ToArray());
        }

        if (fastCombo != null) {
            var combo = fastCombo switch {
                FastCombo.SelectAll => new[] { KeySymbol.Control_L, KeySymbol.a },
                FastCombo.ClearTextBox => new[] { KeySymbol.Control_L, KeySymbol.a },
                FastCombo.Copy => new[] { KeySymbol.Control_L, KeySymbol.c },
                FastCombo.Cut => new[] { KeySymbol.Control_L, KeySymbol.x },
                FastCombo.Paste => new[] { KeySymbol.Control_L, KeySymbol.v },
                _ => Array.Empty<KeySymbol>()
            };
            var secondCombo = fastCombo switch {
                FastCombo.SelectAll => null,
                FastCombo.ClearTextBox => new[] { KeySymbol.BackSpace },
                FastCombo.Copy => null,
                FastCombo.Cut => null,
                FastCombo.Paste => null,
                _ => Array.Empty<KeySymbol>()
            };

            await VncManager.SendKeyCombo(combo);
            if (secondCombo != null)
                await VncManager.SendKeyCombo(secondCombo);
        }

        await Task.Delay(500);
        var stream = await VncManager.GetScreenPNG();
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .AddFile("screen.png", stream));
        await stream.DisposeAsync();
    }
}