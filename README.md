# Anki Importer

Anki Importer makes vocabulary import from ChatGPT to Anki Desktop simple enough for non-technical users.

## Target experience

1. Install **Anki Importer** once.
2. Open Anki Desktop with AnkiConnect enabled.
3. In ChatGPT, attach a `.txt` file and ask:

```text
@anki vocabulary importer Deck "English"
```

4. If the computer is not connected yet, click **Conectar este computador**.
5. The integration parses `Portuguese = English`, checks duplicates against the real Anki deck, adds only new cards, and returns a compact report.

The normal user flow must not require terminal commands, server URLs, tokens, ports, or manual configuration.

## Developer: one-click test deployment

[![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=https%3A%2F%2Fgithub.com%2Fgilbreis%2FAnki-Importer)

The repository includes `render.yaml`, so the test MCP/relay service can be created from the repository without manually entering Docker settings, health checks, or development account configuration.

> The current Blueprint intentionally uses Render's free plan for private end-to-end testing. Its local filesystem is not production persistence. Production distribution will use persistent account/device storage and production identity instead of the development account resolver.

## Architecture

```text
ChatGPT Web
   |
   v
ChatGPT Plugin / App
   |
   v
MCP / Relay Service
   |
   v
Anki Desktop Companion
   |
   v
AnkiConnect (127.0.0.1:8765)
   |
   v
Anki Desktop
   |
   v
AnkiWeb Sync
```

## MVP scope

- Parse vocabulary in the format `portuguese = english`
- Select destination deck
- Validate AnkiConnect availability
- List decks
- Check duplicates by Front field
- Add cards using `Basic` / `Front` / `Back`
- UTF-8 support for Portuguese accents
- Return counts for added, duplicate, invalid, and failed cards
- Keep AnkiConnect local; never expose port `8765` to the internet
- One-click computer pairing through the installed Windows Companion

## Repository structure

```text
chatgpt-plugin/       ChatGPT-facing instructions and integration notes
mcp-server/           Remote MCP/relay service
desktop-companion/    Local Windows bridge to AnkiConnect
shared/schemas/       Shared request/response contracts
installer/            Windows Setup.exe flow
docs/                 Architecture and product documentation
render.yaml           One-click private test deployment
```

## Security principle

AnkiConnect remains bound to localhost (`127.0.0.1:8765`). The Desktop Companion communicates locally with Anki and establishes only outbound authenticated communication to the relay. Device credentials are not placed in WebSocket URLs.

## Current status

The AnkiConnect path has been validated with API version 6 and real UTF-8 `addNote` insertion. The Windows Companion, Setup.exe, one-click pairing flow, MCP build, duplicate checking, and card insertion pipeline are implemented and CI-built. The next milestone is the first hosted end-to-end test from ChatGPT to a real local Anki deck.
