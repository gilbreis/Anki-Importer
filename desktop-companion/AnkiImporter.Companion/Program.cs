using System.Text.Json;
using AnkiImporter.Companion;

using var http = new HttpClient
{
    BaseAddress = new Uri("http://127.0.0.1:8765"),
    Timeout = TimeSpan.FromSeconds(10)
};

var anki = new AnkiConnectClient(http);
var importer = new VocabularyImporter(anki);
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

        case "agent":
        {
            var serverUrl = GetRequiredSetting(args, 1, "ANKI_SERVER_URL", "Server URL is required.");
            var deviceId = GetRequiredSetting(args, 2, "ANKI_DEVICE_ID", "Device id is required.");
            var token = args.Length > 3 ? args[3] : Environment.GetEnvironmentVariable("ANKI_AGENT_TOKEN");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
            };

            var agent = new RemoteAgent(anki, importer);
            await agent.RunForeverAsync(new Uri(serverUrl), deviceId, token, cts.Token);
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

static string GetRequiredSetting(string[] args, int index, string environmentVariable, string error)
{
    var value = args.Length > index ? args[index] : Environment.GetEnvironmentVariable(environmentVariable);
    if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(error);
    return value;
}

static void PrintUsage()
{
    Console.WriteLine("Anki Importer Companion");
    Console.WriteLine("Commands:");
    Console.WriteLine("  health");
    Console.WriteLine("  list-decks");
    Console.WriteLine("  find-duplicates <request.json>");
    Console.WriteLine("  add-cards <request.json>");
    Console.WriteLine("  agent <server-url> <device-id> [token]");
    Console.WriteLine();
    Console.WriteLine("Agent settings can also come from ANKI_SERVER_URL, ANKI_DEVICE_ID, and ANKI_AGENT_TOKEN.");
}
