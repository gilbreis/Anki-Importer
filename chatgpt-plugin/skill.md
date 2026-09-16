# Anki Vocabulary Importer

## Purpose

Generate a local `.ankiimport` package from a user-provided vocabulary text file. The user then opens that file on Windows and the locally installed Anki Importer sends the cards to Anki Desktop through AnkiConnect.

## Invocation

Typical invocation:

```text
Deck "English"
```

with an attached `.txt` file.

The deck name may be any value supplied by the user.

## Expected vocabulary format

Treat lines containing a single vocabulary mapping in this form as candidates:

```text
portuguese = english
```

Examples:

```text
montar = assemble
dividir = split
para-choque = the bumper
```

Ignore transcript metadata such as timestamps, sender names, headers and blank lines unless they are part of a valid mapping.

## Card mapping

- Front: Portuguese text from the left side of `=`.
- Back: English text from the right side of `=`.
- Default model: `Basic`.
- Default fields: `Front` and `Back`.
- Destination deck: exact deck name supplied by the user.

## Normalization

1. Trim leading/trailing whitespace.
2. Collapse repeated internal whitespace.
3. Preserve Portuguese accents and punctuation.
4. Preserve the English translation as supplied unless there is an obvious formatting-only issue.
5. Detect duplicate Front values inside the uploaded batch case-insensitively.
6. Do not invent translations for missing values.

Malformed entries are excluded from the generated package and counted as invalid in the ChatGPT-side preparation summary.

## Required workflow

### 1. Read the attached TXT

Extract only valid `Portuguese = English` mappings.

### 2. Resolve the deck

Use the exact deck name supplied by the user. Do not silently substitute a similarly named deck.

### 3. Prepare the local package

Generate a UTF-8 JSON file with extension `.ankiimport` using this structure:

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

Use a filename based on the deck when practical, for example:

```text
English.ankiimport
Business-English.ankiimport
```

### 4. Return the file

Provide the generated `.ankiimport` file directly to the user.

The user only needs to double-click it after installing `AnkiImporterSetup.exe` once.

### 5. Local importer responsibility

The Windows Anki Importer is responsible for:

- talking only to local AnkiConnect at `127.0.0.1:8765`;
- creating the destination deck if it does not exist;
- checking duplicates already present in the destination deck;
- checking duplicates again locally before insertion;
- adding only new cards;
- never overwriting or deleting existing cards;
- showing the final report to the user;
- exiting after the import completes.

## End-user UX rule

Normal usage must remain:

```text
install once
→ attach TXT in ChatGPT
→ say Deck "..."
→ download .ankiimport
→ double-click
→ done
```

Do not introduce server URLs, Render, MCP hosting, OAuth, login, tokens, PowerShell, ports, WebSockets, pairing codes or any other infrastructure step into the end-user flow.

The solution must remain free to use and local-only apart from the normal ChatGPT interaction used to prepare the package.

## Safety and data handling

- Never expose AnkiConnect port `8765` to the internet.
- Existing cards must not be modified or deleted.
- Preserve UTF-8 accents and punctuation.
- The `.ankiimport` package contains only the requested deck/mapping information and vocabulary cards.

## Future audio behavior

Audio/TTS is not part of the current MVP. When implemented, English pronunciation audio should remain compatible with this local-only flow and must not require a paid service for basic use.
