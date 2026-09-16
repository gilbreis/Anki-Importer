using System.Net.Http.Json;
using System.Text.Json;

namespace AnkiImporter.Companion;

public sealed class PairingClient
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public PairingClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<CompanionConfig> ActivateLinkAsync(
        Uri serverBaseUri,
        string ticket,
        CancellationToken cancellationToken = default)
    {
        if (!serverBaseUri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("O link de conexão não é seguro. Gere um novo link no ChatGPT.");

        if (string.IsNullOrWhiteSpace(ticket))
            throw new InvalidOperationException("O link de conexão está incompleto.");

        using var response = await _http.PostAsJsonAsync(
            new Uri(serverBaseUri, "/pair/link/activate"),
            new { ticket },
            _json,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException("Este link de conexão expirou ou já foi utilizado. Gere um novo link no ChatGPT.");

        var result = await response.Content.ReadFromJsonAsync<LinkActivationResponse>(_json, cancellationToken)
                     ?? throw new InvalidOperationException("O servidor de conexão retornou uma resposta vazia.");

        if (!result.Paired || string.IsNullOrWhiteSpace(result.DeviceId) || string.IsNullOrWhiteSpace(result.DeviceToken))
            throw new InvalidOperationException("Não foi possível concluir a conexão deste computador.");

        return new CompanionConfig(
            serverBaseUri.ToString().TrimEnd('/'),
            result.DeviceId,
            result.DeviceToken,
            DateTimeOffset.UtcNow);
    }

    public async Task<CompanionConfig> PairAsync(
        Uri serverBaseUri,
        Action<string>? showCode = null,
        CancellationToken cancellationToken = default)
    {
        using var startResponse = await _http.PostAsync(new Uri(serverBaseUri, "/pair/start"), content: null, cancellationToken);
        startResponse.EnsureSuccessStatusCode();

        var start = await startResponse.Content.ReadFromJsonAsync<PairStartResponse>(_json, cancellationToken)
                    ?? throw new InvalidOperationException("Pairing server returned an empty response.");

        showCode?.Invoke(start.PairingCode);

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

            var statusUrl = new Uri(serverBaseUri, $"/pair/status?pairingId={Uri.EscapeDataString(start.PairingId)}");
            using var statusResponse = await _http.GetAsync(statusUrl, cancellationToken);

            if (statusResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                throw new InvalidOperationException("Pairing code expired. Start pairing again.");

            statusResponse.EnsureSuccessStatusCode();

            var status = await statusResponse.Content.ReadFromJsonAsync<PairStatusResponse>(_json, cancellationToken)
                         ?? throw new InvalidOperationException("Pairing server returned an empty status response.");

            if (!status.Paired) continue;

            if (string.IsNullOrWhiteSpace(status.DeviceId) || string.IsNullOrWhiteSpace(status.DeviceToken))
                throw new InvalidOperationException("Pairing completed without device credentials.");

            return new CompanionConfig(
                serverBaseUri.ToString().TrimEnd('/'),
                status.DeviceId,
                status.DeviceToken,
                DateTimeOffset.UtcNow);
        }

        throw new OperationCanceledException(cancellationToken);
    }

    private sealed record PairStartResponse(string PairingId, string PairingCode, DateTimeOffset ExpiresAt);
    private sealed record PairStatusResponse(bool Paired, string? DeviceId, string? DeviceToken);
    private sealed record LinkActivationResponse(bool Paired, string? DeviceId, string? DeviceToken);
}
