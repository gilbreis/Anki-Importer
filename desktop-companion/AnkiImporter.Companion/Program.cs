using System.Text.Json;
using AnkiImporter.Companion;

using var ankiHttp = new HttpClient
{
    BaseAddress = new Uri("http://127.0.0.1:8765"),
    Timeout = TimeSpan.FromSeconds(10)
};

using var remoteHttp = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(15)
};

var anki = new AnkiConnectClient(ankiHttp);
var importer = new VocabularyImporter(anki);
var pairingClient = new PairingClient(remoteHttp);
var json = new JsonSerializerOptions(JsonSerializerDefaults.Web)
{
    WriteIndented = true
};

if (args.Length == 0)
{
    PrintUsage();
    return;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "health":
        {
            var version = await anki.InvokeAsync<int>("version");
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                ok = true,
                ankiConnectVersion = version
            }, json));
            break;
        }

        case "list-decks":
        {
            var decks = await anki.InvokeAsync<string[]>("deckNames") ?? [];
            Console.WriteLine(JsonSerializer.Serialize(new { decks }, json));
            break;
        }

        case "find-duplicates":
        {
            var request = await ReadRequestAsync(args);
            var duplicates = await importer.FindDuplicatesAsync(request);
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                deck = request.Deck,
                total = request.Cards.Count,
                duplicates
            }, json));
            break;
        }

        case "add-cards":
        {
            var request = await ReadRequestAsync(args);
            var report = await importer.ImportAsync(request);
            Console.WriteLine(JsonSerializer.Serialize(report, json));
            break;
        }

        case "pair":
        {
            if (args.Length < 2)
                throw new ArgumentException("Usage: pair <server-url>");

            var serverUri = new Uri(args[1], UriKind.Absolute);

            var config = await pairingClient.PairAsync(
                serverUri,
                code =>
                {
                    Console.WriteLine();
                    Console.WriteLine("Pairing code:");
                    Console.WriteLine();
                    Console.WriteLine($"    {code}");
                    Console.WriteLine();
                    Console.WriteLine("In ChatGPT, ask the Anki Importer plugin to pair this code.");
                    Console.WriteLine("Waiting for confirmation...");
                });

            await CompanionConfigStore.SaveAsync(config);

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                paired = true,
                deviceId = config.DeviceId,
                configPath = CompanionConfigStore.ConfigPath
            }, json));
            break;
        }

        case "run":
        {
            var config = await CompanionConfigStore.LoadAsync()
                         ?? throw new InvalidOperationException("Companion is not paired. Run 'pair <server-url>' first.");

            var remoteAgent = new RemoteAgent(anki, importer);
            await remoteAgent.RunForeverAsync(
                new Uri(config.ServerUrl, UriKind.Absolute),
                config.DeviceId,
                config.DeviceToken);
            break;
        }

        case "status":
        {
            var config = await CompanionConfigStore.LoadAsync();
            var ankiOnline = false;
            int? ankiConnectVersion = null;

            try
            {
                ankiConnectVersion = await anki.InvokeAsync<int>("version");
                ankiOnline = true;
            }
            catch
            {
                // Status should still report pairing state when Anki is closed.
            }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                paired = config is not null,
                deviceId = config?.DeviceId,
                serverUrl = config?.ServerUrl,
                pairedAtUtc = config?.PairedAtUtc,
                ankiOnline,
                ankiConnectVersion
            }, json));
            break;
        }

        case "unpair":
        {
            CompanionConfigStore.Delete();
            Console.WriteLine(JsonSerializer.Serialize(new { paired = false }, json));
            break;
        }

        default:
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            PrintUsage();
            Environment.ExitCode = 2;
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(JsonSerializer.Serialize(new
    {
        ok = false,
        error = ex.Message
    }, json));
    Environment.ExitCode = 1;
}

static async Task<ImportRequest> ReadRequestAsync(string[] args)
{
    if (args.Length < 2)
        throw new ArgumentException("A JSON request file path is required.");

    var path = Path.GetFullPath(args[1]);
    if (!File.Exists(path))
        throw new FileNotFoundException("Request file not found.", path);

    await using var stream = File.OpenRead(path);
    var request = await JsonSerializer.DeserializeAsync<ImportRequest>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    return request ?? throw new InvalidOperationException("Invalid import request JSON.");
}

static void PrintUsage()
{
    Console.WriteLine("Anki Importer Companion");
    Console.WriteLine("Commands:");
    Console.WriteLine("  health");
    Console.WriteLine("  status");
    Console.WriteLine("  list-decks");
    Console.WriteLine("  find-duplicates <request.json>");
    Console.WriteLine("  add-cards <request.json>");
    Console.WriteLine("  pair <server-url>");
    Console.WriteLine("  run");
    Console.WriteLine("  unpair");
}
