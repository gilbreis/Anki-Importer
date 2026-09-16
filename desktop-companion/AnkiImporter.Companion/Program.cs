using AnkiImporter.Companion;

using var http = new HttpClient
{
    BaseAddress = new Uri("http://127.0.0.1:8765"),
    Timeout = TimeSpan.FromSeconds(5)
};

var anki = new AnkiConnectClient(http);

if (args.Length == 0)
{
    Console.WriteLine("Anki Importer Companion");
    Console.WriteLine("Commands: health | list-decks");
    return;
}

try
{
    switch (args[0].ToLowerInvariant())
    {
        case "health":
        {
            var version = await anki.InvokeAsync<int>("version");
            Console.WriteLine($"AnkiConnect OK - API version {version}");
            break;
        }

        case "list-decks":
        {
            var decks = await anki.InvokeAsync<string[]>("deckNames") ?? [];
            foreach (var deck in decks)
                Console.WriteLine(deck);
            break;
        }

        default:
            Console.Error.WriteLine($"Unknown command: {args[0]}");
            Environment.ExitCode = 2;
            break;
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
