# ChatGPT Plugin / App

ChatGPT-facing layer for Anki Importer.

## Intended user experience

User installs the Anki Importer plugin/app, pairs the Desktop Companion once, then attaches a `.txt` file and asks:

```text
@anki vocabulary importer Deck "English"
```

The integration should:

1. Parse lines in `Portuguese = English` format.
2. Ignore timestamps/chat metadata around valid vocabulary lines.
3. Normalize whitespace while preserving accents.
4. Detect duplicates inside the uploaded file.
5. Verify the active paired Anki Desktop.
6. Ask the MCP service for real duplicates in the destination deck.
7. Add only new cards.
8. Return a compact import report.

## Default mapping

```text
Portuguese -> Front
English    -> Back
Model      -> Basic
```

## Files in this directory

- `skill.md` — reusable workflow/instructions for the vocabulary import behavior.
- `app-setup.md` — MCP endpoint, authentication phases, pairing and testing setup.

## MCP tools currently available

```text
claim_anki_pairing
list_anki_devices
select_anki_device
anki_health
list_anki_decks
find_vocabulary_duplicates
add_vocabulary_cards
```

## MVP response example

```text
Deck: English
Found: 18
Added: 15
Duplicates: 3
Invalid: 0
Errors: 0
```

## Current implementation status

### Implemented

- Remote MCP server.
- Per-account device isolation.
- Persistent device registry.
- Short-code pairing.
- Outbound authenticated Companion WebSocket.
- AnkiConnect health/deck/duplicate/add operations.
- Windows-protected Companion credential storage.
- Windows self-contained build workflow.
- ChatGPT workflow skill.
- Dockerized MCP server.
- CI typecheck/build for MCP.

### Required before private ChatGPT test

1. Deploy the MCP server to a stable HTTPS host.
2. Persist `/app/data` for the device registry.
3. Configure a private development account credential.
4. Connect the hosted `/mcp` endpoint as a custom ChatGPT app in Developer Mode where supported.
5. Install/pair the Windows Companion.
6. Run an end-to-end import from an attached TXT file.

### Required before public distribution

1. Replace prototype Bearer-token account resolution with production OAuth identity.
2. Add device revocation and account-data deletion flows.
3. Finalize privacy/support contact information.
4. Sign/package the Windows Companion installer.
5. Add production monitoring, rate limits, secure secret management and backups.
6. Complete app/plugin submission and review requirements.

## Future

- English TTS/audio on Back.
- Preview/approval mode.
- Configurable model/field mapping.
- Device display names and revocation UI.
- Automatic Companion updates.
