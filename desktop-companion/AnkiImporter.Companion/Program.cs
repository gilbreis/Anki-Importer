using System.Text.Json;
using System.Windows.Forms;
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

try
{
    if (args.Length > 0 && Uri.TryCreate(args[0], UriKind.Absolute, out var protocolUri) &&
        protocolUri.Scheme.Equals("anki-importer", StringComparison.OrdinalIgnoreCase))
    {
        if (!protocolUri.Host.Equals("pair", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Link do Anki Importer não reconhecido.");

        var query = System.Web.HttpUtility.ParseQueryString(protocolUri.Query);
        var server = query["server"];
        var ticket = query["ticket"];

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(ticket))
            throw new InvalidOperationException("Link de conexão incompleto.");

        var config = await pairingClient.ActivateLinkAsync(new Uri(server, UriKind.Absolute), ticket);
        await CompanionConfigStore.SaveAsync(config);

        MessageBox.Show(
            "Este computador foi conectado ao Anki Importer com sucesso.",
            "Anki Importer",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);

        var remoteAgent = new RemoteAgent(anki, importer);
        await remoteAgent.RunForeverAsync(new Uri(config.ServerUrl), config.DeviceId, config.DeviceToken);
        return;
    }

    if (args.Length == 0)
    {
        var config = await CompanionConfigStore.LoadAsync();
        if (config is null)
        {
            MessageBox.Show(
                "O Anki Importer ainda não foi conectado ao ChatGPT. Abra o app Anki Importer no ChatGPT e clique em Conectar este computador.",
                "Anki Importer",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var remoteAgent = new RemoteAgent(anki, importer);
        await remoteAgent.RunForeverAsync(
            new Uri(config.ServerUrl, UriKind.Absolute),
            config.DeviceId,
            config.DeviceToken);
        return;
    }

    switch (args[0].ToLowerInvariant())
    {
        case "setup":
        {
            if (args.Length < 2)
                throw new ArgumentException("Setup requires the service URL supplied by the installer.");

            ApplicationConfiguration.Initialize();
            using var wizard = new SetupWizard(anki, pairingClient, new Uri(args[1], UriKind.Absolute));
            Application.Run(wizard);
            break;
        }

        case "health":
        {
            var version = await anki.InvokeAsync<int>("version");
            Console.WriteLine(JsonSerializer.Serialize(new { ok = true, ankiConnectVersion = version }, json));
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

            var config = await pairingClient.PairAsync(new Uri(args[1], UriKind.Absolute), Console.WriteLine);
            await CompanionConfigStore.SaveAsync(config);
            Console.WriteLine(JsonSerializer.Serialize(new { paired = true, deviceId = config.DeviceId }, json));
            break;
        }

        case "run":
        {
            var config = await CompanionConfigStore.LoadAsync()
                         ?? throw new InvalidOperationException("Companion is not paired.");
            var remoteAgent = new RemoteAgent(anki, importer);
            await remoteAgent.RunForeverAsync(new Uri(config.ServerUrl), config.DeviceId, config.DeviceToken);
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
            catch { }

            Console.WriteLine(JsonSerializer.Serialize(new
            {
                paired = config is not null,
                deviceId = config?.DeviceId,
                pairedAtUtc = config?.PairedAtUtc,
                ankiOnline,
                ankiConnectVersion
            }, json));
            break;
        }

        case "unpair":
            CompanionConfigStore.Delete();
            break;

        default:
            throw new ArgumentException("Unknown internal command.");
    }
}
catch (Exception ex)
{
    if (args.Length > 0 &&
        (args[0].Equals("setup", StringComparison.OrdinalIgnoreCase) || args[0].StartsWith("anki-importer://", StringComparison.OrdinalIgnoreCase)))
    {
        MessageBox.Show(ex.Message, "Anki Importer", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    else
    {
        Console.Error.WriteLine(JsonSerializer.Serialize(new { ok = false, error = ex.Message }, json));
    }

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
