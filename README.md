# Anki Importer

Anki Importer is a project to make vocabulary import from ChatGPT to Anki Desktop simple enough for non-technical users.

## Target experience

1. User installs the ChatGPT plugin/app.
2. User installs the local Anki Desktop Companion once.
3. User opens Anki Desktop with AnkiConnect enabled.
4. In ChatGPT, the user sends a `.txt` file and asks:

```text
@anki vocabulary importer Deck "English"
```

5. The integration parses `Portuguese = English`, checks duplicates against the real Anki deck, adds only new cards, and returns a report.

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
- Keep AnkiConnect local; do not expose port `8765` to the internet

## Repository structure

```text
chatgpt-plugin/       ChatGPT-facing instructions and integration notes
mcp-server/           Remote MCP/relay service
desktop-companion/    Local Windows bridge to AnkiConnect
shared/schemas/       Shared request/response contracts
installer/            One-time desktop installation flow
docs/                 Architecture and product documentation
```

## Security principle

AnkiConnect remains bound to localhost (`127.0.0.1:8765`). The desktop companion is responsible for local communication with Anki and should establish outbound authenticated communication when remote connectivity is introduced.

## Current status

Initial MVP scaffold. The AnkiConnect path has already been validated manually with API version 6 and a successful `addNote` operation using UTF-8 JSON bytes.
