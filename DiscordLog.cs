using DSharpPlus;
using DSharpPlus.Entities;

namespace DisControl; 

public static class DiscordLog {
    private static List<DiscordChannel> _channels = new();
    private static DiscordClient _client;
    
    public static async Task Load(DiscordClient? client = null) {
        client ??= _client; _client = client;
        var toRemove = new List<Configuration.DiscordClass.GuildChannel>();
        foreach (var i in Configuration.Instance.Discord.LogChannels) {
            try {
                var guild = await client.GetGuildAsync(i.Guild);
                var channel = guild.GetChannel(i.Channel);
                _channels.Add(channel);
            } catch {
                toRemove.Add(i);
            }
        }

        foreach (var i in toRemove)
            Configuration.Instance.Discord.LogChannels.Remove(i);
    }

    public static async Task Send(DiscordEmbed embed) {
        foreach (var i in _channels)
            await new DiscordMessageBuilder()
                .WithEmbed(embed).SendAsync(i);
    }
}