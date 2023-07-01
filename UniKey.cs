using System.Text;
using MarcusW.VncClient;
using Serilog;

namespace DisControl; 

public static class UniKey {
    private static Dictionary<char, KeySymbol> _table = new();

    static UniKey() {
        Log.Information("[UniKey] Initializing KeySym <-> Unicode table");
        if (!File.Exists("unikey.txt")) {
            using var client = new HttpClient();
            var str = client.GetStringAsync(
                    "https://www.cl.cam.ac.uk/~mgk25/ucs/keysyms.txt")
                .ConfigureAwait(false).GetAwaiter().GetResult();
            File.WriteAllText("unikey.txt", str);
        }

        var file = File.ReadAllText("unikey.txt");
        using var reader = new StringReader(file);
        while (reader.ReadLine() is { } line) {
            Console.WriteLine(line);
            if (line.StartsWith("#")) continue; // Skip any comments
            if (string.IsNullOrWhiteSpace(line)) continue; // Kill yourself omg
            if (line.StartsWith("0xffffff")) continue; // Man just kill yourself already
            var split = line.Split("   "); // Split line by 3 spaces
            if (split[1] == "U0000") continue; // No matching unicode character for KeySym
            var keysym = (KeySymbol)Convert.ToInt32(split[0][2..], 16);
            var unicode = Encoding.Unicode.GetString(Convert
                .FromHexString(split[1][1..]));
            _table.TryAdd(unicode[0], keysym);
        }
    }

    public static KeySymbol FromUnicode(char unicode)
        => _table[unicode];
}