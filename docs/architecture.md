# Architecture

## Goal

Allow a user to install an Anki integration in ChatGPT, attach a vocabulary `.txt` file, choose a deck, and have new cards added to Anki Desktop with minimal setup.

## Components

### 1. ChatGPT Plugin / App
Responsibilities:
- Accept the user command and attached text file.
- Parse vocabulary lines such as `montar = assemble`.
- Normalize whitespace and reject malformed entries.
- Ask the backend to inspect the user's real Anki collection.
- Present a concise import report.

The ChatGPT side must not attempt to access `127.0.0.1:8765` directly.

### 2. MCP / Relay Service
Responsibilities:
- Expose tools to the ChatGPT app.
- Authenticate users and desktop companions.
- Route requests to the correct connected companion.
- Never expose the local AnkiConnect port to the internet.

Initial tool contract:
- `health`
- `list_decks`
- `find_duplicates`
- `add_cards`

### 3. Anki Desktop Companion
Responsibilities:
- Run on the user's Windows machine.
- Connect locally to AnkiConnect at `http://127.0.0.1:8765`.
- Establish an outbound authenticated connection to the relay.
- Translate relay commands to AnkiConnect actions.
- Return structured results.

### 4. AnkiConnect
AnkiConnect remains local-only and is the supported bridge into Anki Desktop.

## MVP import flow

```text
User attaches TXT in ChatGPT
        |
        v
Parse Portuguese = English
        |
        v
Send normalized cards + deck to relay
        |
        v
Relay routes to user's desktop companion
        |
        v
Companion queries AnkiConnect
        |
        +--> deckNames
        +--> findNotes / notesInfo
        +--> addNotes
        |
        v
Structured result returned to ChatGPT
```

## Duplicate policy

For MVP, duplicate detection is based on a normalized `Front` value within the destination deck:
- trim leading/trailing whitespace
- collapse repeated whitespace
- compare case-insensitively

Existing cards are not modified automatically.

## Encoding

All JSON exchanged with AnkiConnect must be sent as UTF-8 bytes. This avoids Windows PowerShell encoding problems with Portuguese characters such as `ç`, `ã`, and `é`.

## Security

- Keep AnkiConnect bound to localhost.
- Do not expose port 8765 through router forwarding, tunnels, or public bind addresses.
- Desktop companion should initiate outbound connectivity.
- Pairing between ChatGPT account/session and desktop companion should require a short-lived code or equivalent explicit user authorization.
- Never transmit the entire Anki collection when a scoped query is sufficient.

## Later phases

- TTS generation and media upload
- Configurable note types and fields
- macOS/Linux companion
- Import history
- Auto-update for the companion
- Signed installer
