using System.Text.Json;

namespace AnkiImporter.Companion;

public sealed class VocabularyImporter
{
    private readonly AnkiConnectClient _anki;

    public VocabularyImporter(AnkiConnectClient anki)
    {
        _anki = anki;
    }

    public async Task<ImportReport> ImportAsync(ImportRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Deck))
            throw new ArgumentException("Deck is required.", nameof(request));

        var deckNames = await _anki.InvokeAsync<string[]>("deckNames", cancellationToken: cancellationToken) ?? [];
        if (!deckNames.Contains(request.Deck, StringComparer.Ordinal))
            await _anki.InvokeAsync<long>("createDeck", new { deck = request.Deck }, cancellationToken);

        var modelNames = await _anki.InvokeAsync<string[]>("modelNames", cancellationToken: cancellationToken) ?? [];
        if (!modelNames.Contains(request.Model, StringComparer.Ordinal))
            throw new InvalidOperationException($"Anki model '{request.Model}' was not found.");

        var modelFields = await _anki.InvokeAsync<string[]>("modelFieldNames", new { modelName = request.Model }, cancellationToken) ?? [];
        if (!modelFields.Contains(request.FrontField, StringComparer.Ordinal) ||
            !modelFields.Contains(request.BackField, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"Model '{request.Model}' must contain fields '{request.FrontField}' and '{request.BackField}'. " +
                $"Available fields: {string.Join(", ", modelFields)}");
        }

        var escapedDeck = EscapeQueryValue(request.Deck);
        var noteIds = await _anki.InvokeAsync<long[]>("findNotes", new { query = $"deck:\"{escapedDeck}\"" }, cancellationToken) ?? [];

        var existingFronts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (noteIds.Length > 0)
        {
            var notes = await _anki.InvokeAsync<AnkiNoteInfo[]>("notesInfo", new { notes = noteIds }, cancellationToken) ?? [];
            foreach (var note in notes)
            {
                if (note.Fields.TryGetValue(request.FrontField, out var field))
                {
                    var normalized = Normalize(field.Value);
                    if (!string.IsNullOrWhiteSpace(normalized))
                        existingFronts.Add(normalized);
                }
            }
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var duplicates = new List<SkippedCard>();
        var invalid = new List<SkippedCard>();
        var errors = new List<ImportError>();
        var candidates = new List<ImportCard>();

        foreach (var card in request.Cards ?? [])
        {
            if (string.IsNullOrWhiteSpace(card.Front) || string.IsNullOrWhiteSpace(card.Back))
            {
                invalid.Add(new SkippedCard(card.Front ?? string.Empty, "front_or_back_empty"));
                continue;
            }

            var normalizedFront = Normalize(card.Front);

            if (!seen.Add(normalizedFront))
            {
                duplicates.Add(new SkippedCard(card.Front, "duplicate_in_request"));
                continue;
            }

            if (existingFronts.Contains(normalizedFront))
            {
                duplicates.Add(new SkippedCard(card.Front, "already_exists_in_deck"));
                continue;
            }

            candidates.Add(card);
        }

        var added = new List<AddedCard>();
        if (candidates.Count > 0)
        {
            var notes = candidates.Select(card => new
            {
                deckName = request.Deck,
                modelName = request.Model,
                fields = new Dictionary<string, string>
                {
                    [request.FrontField] = card.Front.Trim(),
                    [request.BackField] = card.Back.Trim()
                },
                options = new { allowDuplicate = false },
                tags = new[] { "chatgpt-import" }
                    .Concat(card.Tags ?? [])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            }).ToArray();

            var result = await _anki.InvokeAsync<long?[]>("addNotes", new { notes }, cancellationToken) ?? [];

            for (var i = 0; i < candidates.Count; i++)
            {
                var card = candidates[i];
                if (i < result.Length && result[i] is long noteId)
                    added.Add(new AddedCard(noteId, card.Front, card.Back));
                else
                    errors.Add(new ImportError(card.Front, "addNotes_returned_null"));
            }
        }

        return new ImportReport(
            request.Deck,
            request.Cards?.Count ?? 0,
            added,
            duplicates,
            invalid,
            errors);
    }

    public async Task<IReadOnlyList<SkippedCard>> FindDuplicatesAsync(
        ImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var report = await ImportAsync(request with { Cards = request.Cards }, cancellationToken);

        if (report.Added.Count > 0)
            throw new InvalidOperationException("FindDuplicatesAsync must not add cards. Use the dedicated duplicate query path.");

        return report.Duplicates;
    }

    public async Task<HashSet<string>> GetExistingFrontsAsync(
        string deck,
        string frontField = "Front",
        CancellationToken cancellationToken = default)
    {
        var escapedDeck = EscapeQueryValue(deck);
        var noteIds = await _anki.InvokeAsync<long[]>("findNotes", new { query = $"deck:\"{escapedDeck}\"" }, cancellationToken) ?? [];
        var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (noteIds.Length == 0)
            return existing;

        var notes = await _anki.InvokeAsync<AnkiNoteInfo[]>("notesInfo", new { notes = noteIds }, cancellationToken) ?? [];
        foreach (var note in notes)
        {
            if (note.Fields.TryGetValue(frontField, out var field))
            {
                var normalized = Normalize(field.Value);
                if (!string.IsNullOrWhiteSpace(normalized))
                    existing.Add(normalized);
            }
        }

        return existing;
    }

    private static string Normalize(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();

    private static string EscapeQueryValue(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal);
}
