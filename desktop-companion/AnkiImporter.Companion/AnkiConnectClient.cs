using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace AnkiImporter.Companion;

public sealed class AnkiConnectClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public AnkiConnectClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<T?> InvokeAsync<T>(string action, object? parameters = null, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            action,
            version = 6,
            @params = parameters ?? new { }
        };

        var json = JsonSerializer.Serialize(payload, _json);
        using var content = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
        content.Headers.ContentType = new("application/json") { CharSet = "utf-8" };

        using var response = await _http.PostAsync("/", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<AnkiEnvelope<T>>(_json, cancellationToken)
                   ?? throw new InvalidOperationException("Empty response from AnkiConnect.");

        if (!string.IsNullOrWhiteSpace(body.Error))
            throw new InvalidOperationException($"AnkiConnect error: {body.Error}");

        return body.Result;
    }

    private sealed record AnkiEnvelope<T>(T? Result, string? Error);
}
