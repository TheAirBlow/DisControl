using Serilog;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DisControl; 

public class Configuration {
    public static Configuration Instance;

    static Configuration() {
        Log.Information("[Configuration] Parsing configuration file");
        if (File.Exists("config.yaml")) {
            var content = File.ReadAllText("config.yaml");
            try {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(CamelCaseNamingConvention
                        .Instance).Build();
                Instance = deserializer.Deserialize<Configuration>(content);
            } catch (Exception e) {
                Log.Fatal("Failed to parse configuration file!");
                Log.Fatal("{0}", e);
                Environment.Exit(-1);
            }
            return;
        }

        Instance = new Configuration();
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention
                .Instance).Build();
        var blank = serializer.Serialize(Instance);
        File.WriteAllText("config.yaml", blank);
        Log.Fatal("No configuration file was found, created an blank one!");
        Log.Fatal("Populate it with all information required.");
        Environment.Exit(-1);
    }

    public class DiscordClass {
        public class GuildChannel {
            public ulong Guild;
            public ulong Channel;
        }
        
        public List<GuildChannel> CommandChannels = new();
        public List<GuildChannel> LogChannels = new();
        public List<ulong> Admins = new();
        public string BotToken = "";
    }

    public class VirtualMachine {
        public string Identifier = Guid.NewGuid().ToString();
        public string DisplayName = "";
        public long Memory;
    }

    public class OtherClass {
        public int? HoursUntilReset;
        public string? ResetSource;
        public string? ResetTarget;
        public bool PeriodicReset;
        public bool AutoRestart;
    }

    public List<VirtualMachine> VirtualMachines = new();
    public DiscordClass Discord = new();
    public OtherClass Other = new();

    public static void SaveChanges() {
        var serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention
                .Instance).Build();
        var config = serializer.Serialize(Instance);
        File.WriteAllText("config.yaml", config);
    }
}