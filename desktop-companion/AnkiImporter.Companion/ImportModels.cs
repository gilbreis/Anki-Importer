using System.Text.Json.Serialization;

namespace AnkiImporter.Companion;

public sealed record ImportRequest(
    string Deck,
    IReadOnlyList<ImportCard> Cards,
    string Model = "Basic",
    string FrontField = "Front",
    string BackField = "Back");

public sealed record ImportCard(string Front, string Back, string[]? Tags = null);

public sealed record ImportReport(
    string Deck,
    int Total,
    IReadOnlyList<AddedCard> Added,
    IReadOnlyList<SkippedCard> Duplicates,
    IReadOnlyList<SkippedCard> Invalid,
    IReadOnlyList<ImportError> Errors);

public sealed record AddedCard(long Id, string Front, string Back);
public sealed record SkippedCard(string Front, string Reason);
public sealed record ImportError(string? Front, string Reason);

internal sealed record AnkiNoteInfo(
    long NoteId,
    Dictionary<string, AnkiField> Fields);

internal sealed record AnkiField(string Value, int Order);
