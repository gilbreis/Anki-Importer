# Desktop Companion

Local Windows component that bridges the remote Anki Importer service to Anki Desktop through AnkiConnect.

## MVP responsibilities

- Verify AnkiConnect availability at `http://127.0.0.1:8765`.
- Report AnkiConnect API version.
- List available decks.
- Find duplicates by normalized Front value inside a selected deck.
- Add new notes using `addNotes`.
- Send and receive JSON as UTF-8.
- Keep port 8765 local-only.

## Planned local API abstraction

The companion should expose internal handlers for:

```text
health
list_decks
find_duplicates
add_cards
```

These handlers should be transport-agnostic so the relay transport can change later without rewriting the Anki logic.

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
