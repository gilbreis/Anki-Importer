# ChatGPT App — Anki Importer

ChatGPT-facing layer for Anki Importer.

## End-user experience

The product must hide all infrastructure details from the user.

First-time setup:

1. Install the Anki Importer app in ChatGPT.
2. Download and run `AnkiImporterSetup.exe`.
3. Complete the standard Windows wizard: Next → Install → Finish.
4. Return to ChatGPT and click **Connect this computer / Conectar este computador**.
5. The browser opens the installed Companion through the registered `anki-importer://` protocol.
6. Pairing completes automatically using a short-lived, single-use ticket.

The end user must never be asked to configure a server URL, port, token, environment variable, PowerShell command or manual pairing code during the normal flow.

Normal use:

```text
@anki vocabulary importer Deck "English"
```

with an attached `.txt` file.

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

## MCP tools

```text
create_anki_connection_link
claim_anki_pairing          # fallback/development only
list_anki_devices
select_anki_device
revoke_anki_device
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

## Implemented

- Remote MCP server.
- Per-account device isolation.
- Persistent device registry.
- One-click pairing tickets.
- HTTPS handoff page with installer fallback.
- Windows `anki-importer://` custom protocol.
- Automatic ticket activation in the Companion.
- Device listing, selection and revocation.
- Immediate disconnect on device revocation.
- Outbound authenticated Companion WebSocket.
- Device token sent via Authorization header rather than query string.
- Response-to-device binding for WebSocket commands.
- AnkiConnect health/deck/duplicate/add operations.
- Windows-protected Companion credential storage.
- Graphical WinForms first-run wizard.
- Silent normal Windows startup.
- Self-contained Windows build workflow.
- Inno Setup `AnkiImporterSetup.exe` workflow.
- GitHub Release workflow for the installer.
- Dockerized MCP server.
- MCP typecheck/build CI.

## Developer-side work before private end-to-end test

1. Deploy the MCP server to a stable HTTPS host with WebSocket support.
2. Persist the device registry storage.
3. Configure `ANKI_PUBLIC_BASE_URL`.
4. Connect the hosted `/mcp` endpoint to ChatGPT for private testing.
5. Publish a private/test Windows installer release.
6. Run an end-to-end TXT import.

These are deployment tasks and must not become end-user setup steps.

## Before public distribution

1. Replace prototype Bearer account resolution with production OAuth identity.
2. Code-sign the Windows installer/executable.
3. Finalize privacy/support contact information.
4. Add production monitoring, rate limits, secure secret management and backups.
5. Complete ChatGPT app review/distribution requirements.

## Future

- English TTS/audio on Back.
- Preview/approval mode.
- Configurable model/field mapping.
- Friendly device display names.
- Automatic Companion updates.
