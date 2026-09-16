using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace AnkiImporter.Companion;

public sealed class RemoteAgent
{
    private readonly AnkiConnectClient _anki;
    private readonly VocabularyImporter _importer;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public RemoteAgent(AnkiConnectClient anki, VocabularyImporter importer)
    {
        _anki = anki;
        _importer = importer;
    }

    public async Task RunForeverAsync(
        Uri serverBaseUri,
        string deviceId,
        string? token,
        CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromSeconds(2);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await RunSessionAsync(serverBaseUri, deviceId, token, cancellationToken);
                delay = TimeSpan.FromSeconds(2);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Remote agent disconnected: {ex.Message}");
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }
    }

    private async Task RunSessionAsync(
        Uri serverBaseUri,
        string deviceId,
        string? token,
        CancellationToken cancellationToken)
    {
        using var ws = new ClientWebSocket();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Device token is required.");

        ws.Options.SetRequestHeader("Authorization", $"Bearer {token}");
        var endpoint = BuildWebSocketUri(serverBaseUri, deviceId);

        Console.Error.WriteLine($"Connecting to {endpoint.GetLeftPart(UriPartial.Path)} as device '{deviceId}'...");
        await ws.ConnectAsync(endpoint, cancellationToken);
        Console.Error.WriteLine("Remote agent connected.");

        while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var text = await ReceiveTextAsync(ws, cancellationToken);
            if (text is null) break;

            await HandleMessageAsync(ws, text, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(ClientWebSocket ws, string raw, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (!root.TryGetProperty("type", out var type) || type.GetString() != "command")
            return;

        var id = root.GetProperty("id").GetString()
                 ?? throw new InvalidOperationException("Command id is required.");
        var action = root.GetProperty("action").GetString()
                     ?? throw new InvalidOperationException("Command action is required.");

        try
        {
            object result = action switch
            {
                "health" => await HealthAsync(cancellationToken),
                "list-decks" => await ListDecksAsync(cancellationToken),
                "find-duplicates" => await FindDuplicatesAsync(root, cancellationToken),
                "add-cards" => await AddCardsAsync(root, cancellationToken),
                _ => throw new InvalidOperationException($"Unsupported action: {action}")
            };

            await SendJsonAsync(ws, new
            {
                type = "result",
                id,
                ok = true,
                result
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await SendJsonAsync(ws, new
            {
                type = "result",
                id,
                ok = false,
                error = ex.Message
            }, cancellationToken);
        }
    }

    private async Task<object> HealthAsync(CancellationToken cancellationToken)
    {
        var version = await _anki.InvokeAsync<int>("version", cancellationToken: cancellationToken);
        return new { ok = true, deviceOnline = true, ankiConnectVersion = version };
    }

    private async Task<object> ListDecksAsync(CancellationToken cancellationToken)
    {
        var decks = await _anki.InvokeAsync<string[]>("deckNames", cancellationToken: cancellationToken) ?? [];
        return new { decks };
    }

    private async Task<object> FindDuplicatesAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var request = DeserializePayload(root);
        var duplicates = await _importer.FindDuplicatesAsync(request, cancellationToken);
        return new
        {
            deck = request.Deck,
            total = request.Cards.Count,
            duplicates
        };
    }

    private async Task<object> AddCardsAsync(JsonElement root, CancellationToken cancellationToken)
    {
        var request = DeserializePayload(root);
        return await _importer.ImportAsync(request, cancellationToken);
    }

    private ImportRequest DeserializePayload(JsonElement root)
    {
        if (!root.TryGetProperty("payload", out var payload) || payload.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            throw new InvalidOperationException("Command payload is required.");

        return payload.Deserialize<ImportRequest>(_json)
               ?? throw new InvalidOperationException("Invalid import payload.");
    }

    private async Task SendJsonAsync(ClientWebSocket ws, object value, CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, _json));
        await ws.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string?> ReceiveTextAsync(ClientWebSocket ws, CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        using var stream = new MemoryStream();

        while (true)
        {
            var result = await ws.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                return null;

            if (result.MessageType != WebSocketMessageType.Text)
                continue;

            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    private static Uri BuildWebSocketUri(Uri baseUri, string deviceId)
    {
        var builder = new UriBuilder(baseUri)
        {
            Scheme = baseUri.Scheme switch
            {
                "https" => "wss",
                "http" => "ws",
                "wss" => "wss",
                "ws" => "ws",
                _ => throw new ArgumentException("Server URL must use http, https, ws, or wss.")
            },
            Path = "/agent",
            Query = $"deviceId={Uri.EscapeDataString(deviceId)}"
        };

        return builder.Uri;
    }
}
