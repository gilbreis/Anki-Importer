# ChatGPT App Setup

## Goal

Connect the Anki Importer remote MCP server to ChatGPT and bundle the vocabulary-import workflow as a plugin/app experience.

## Remote MCP endpoint

Production shape:

```text
https://<anki-importer-host>/mcp
```

ChatGPT must connect to the remote MCP endpoint. The user's Anki Desktop does not expose an inbound internet port; the Desktop Companion creates the outbound WebSocket connection to the same hosted service.

## Authentication phases

### Phase 1 — private development

Use a static Bearer credential for a development account.

Server environment example:

```text
ANKI_ACCOUNT_TOKENS={"<development-secret>":"gil-dev"}
```

The MCP client sends:

```http
Authorization: Bearer <development-secret>
```

This is suitable only for controlled development/testing. Never commit the secret.

### Phase 2 — public distribution

Replace `AccountAuth.ts` with OAuth-backed identity while keeping the resulting `accountId` contract unchanged.

The rest of the server already routes:

```text
accountId -> active device -> Companion -> AnkiConnect
```

so OAuth does not require changes to vocabulary import logic or device routing.

## App tools

The connected MCP app exposes:

- `claim_anki_pairing`
- `list_anki_devices`
- `select_anki_device`
- `anki_health`
- `list_anki_decks`
- `find_vocabulary_duplicates`
- `add_vocabulary_cards`

## Plugin skill

Bundle or associate `skill.md` with the connected app so the user-facing workflow understands:

```text
@anki vocabulary importer Deck "English"
```

plus a user-provided `.txt` file.

## First-run user experience

1. User installs the Anki Importer plugin/app in ChatGPT.
2. User installs Anki Desktop and AnkiConnect.
3. User installs the Anki Importer Desktop Companion once.
4. Companion displays a short pairing code.
5. User provides the code in ChatGPT.
6. ChatGPT calls `claim_anki_pairing`.
7. Companion receives and stores its device credential using Windows-protected storage.
8. Future imports require no pairing or local commands.

## Import user experience

```text
@anki vocabulary importer Deck "English"
```

Attach a TXT file containing entries such as:

```text
montar = assemble
dividir = split
para-choque = the bumper
```

The app should check connection, parse the file, check duplicates, add only new cards, and report the result.
