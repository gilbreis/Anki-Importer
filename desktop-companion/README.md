# Desktop Companion

Local Windows component that bridges the remote Anki Importer service to Anki Desktop through AnkiConnect.

## Current MVP responsibilities

- Verify AnkiConnect availability at `http://127.0.0.1:8765`.
- Report AnkiConnect API version.
- List available decks.
- Find duplicates by normalized Front value inside a selected deck.
- Add new notes using `addNotes`.
- Return a structured JSON report.
- Send and receive JSON as UTF-8.
- Keep port 8765 local-only.

## Implemented commands

```text
health
list-decks
find-duplicates <request.json>
add-cards <request.json>
```

All commands return JSON so the MCP/relay layer can consume them without parsing human-oriented text.

## Local test

Requirements:

- Windows or another machine with .NET 8 SDK for development
- Anki Desktop open
- AnkiConnect installed and active

From the repository root:

```powershell
dotnet run --project desktop-companion/AnkiImporter.Companion -- health
```

Expected shape:

```json
{
  "ok": true,
  "ankiConnectVersion": 6
}
```

List decks:

```powershell
dotnet run --project desktop-companion/AnkiImporter.Companion -- list-decks
```

Check duplicates without writing anything:

```powershell
dotnet run --project desktop-companion/AnkiImporter.Companion -- find-duplicates desktop-companion/examples/import-request.json
```

Import only new cards:

```powershell
dotnet run --project desktop-companion/AnkiImporter.Companion -- add-cards desktop-companion/examples/import-request.json
```

Example request:

```json
{
  "deck": "English",
  "model": "Basic",
  "frontField": "Front",
  "backField": "Back",
  "cards": [
    { "front": "montar", "back": "assemble" },
    { "front": "dividir", "back": "split" }
  ]
}
```

## Duplicate rules

The MVP normalizes Front values by trimming, collapsing whitespace, and comparing case-insensitively.

A card is skipped when:

- its Front already exists in the selected Anki deck;
- the same Front appears more than once in the same request;
- Front or Back is empty.

Existing cards are never overwritten by the MVP.

## Verified AnkiConnect behavior

The project has manually validated:

- `version` returning API version 6
- `deckNames`
- `addNote`
- UTF-8 JSON byte payloads for Portuguese text

## MVP technology

Initial implementation target: .NET 8 worker/tray application for Windows.

Why .NET:

- native Windows deployment
- straightforward single-file publishing
- built-in HTTP/WebSocket support
- easier signed installer path later
- no Python or Node runtime prerequisite for end users

## Next layer

The next milestone is the MCP/relay transport. It will call the same logical operations remotely while AnkiConnect remains local-only on `127.0.0.1:8765`.
