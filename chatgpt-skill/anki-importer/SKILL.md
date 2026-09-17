---
name: anki-importer
description: Generate .ankiimport files for Anki vocabulary imports from attached TXT files or pasted text containing pairs in the form "Portuguese = English". Use this skill when the user mentions Anki, a deck name, vocabulary import, TXT vocabulary, or writes a short instruction such as Deck "English" with a vocabulary file attached. The skill should automatically parse the vocabulary, remove duplicate entries from the request, and create a downloadable .ankiimport package compatible with the Anki Importer Windows app.
---

# Anki Importer

Create `.ankiimport` files for the local Anki Importer application.

## Trigger behavior

When the user attaches a TXT file containing vocabulary and provides a deck name, including short messages such as:

```text
Deck "English"
```

or:

```text
Deck English
```

immediately process the attached file. Do not ask the user to repeat a longer instruction explaining the `.ankiimport` format.

If a vocabulary TXT is attached and the user clearly asks to import it into Anki but does not provide a deck name, ask only for the deck name.

## Input format

Treat clean lines in the following format as vocabulary pairs:

```text
português = english
```

Examples:

```text
montar = assemble
dividir = split
para-choque = the bumper
```

Ignore chat metadata, timestamps, sender names, empty lines, headings, and unrelated text.

Split each valid vocabulary line at the first `=` character only.

Normalize surrounding whitespace while preserving accents and meaningful internal text.

## Validation and duplicates

For every candidate pair:

1. Require a non-empty left side and non-empty right side.
2. Use the Portuguese/front value as the request-level duplicate key.
3. Compare duplicate keys case-insensitively after trimming and whitespace normalization.
4. Keep the first occurrence and discard later duplicate occurrences from the same input.
5. Do not claim to know whether the card already exists in the user's Anki collection. The Windows Anki Importer performs that check locally when the `.ankiimport` file is opened.

## Package format

Generate a UTF-8 JSON file with extension `.ankiimport` using this schema:

```json
{
  "deck": "English",
  "model": "Basic",
  "frontField": "Front",
  "backField": "Back",
  "tts": true,
  "cards": [
    {
      "front": "montar",
      "back": "assemble"
    }
  ]
}
```

Rules:

- `deck`: use exactly the deck requested by the user.
- `model`: always `Basic` unless the user explicitly asks for another supported model.
- `frontField`: `Front`.
- `backField`: `Back`.
- `tts`: `true` by default.
- `cards[].front`: Portuguese.
- `cards[].back`: English.
- Do not embed executable instructions, URLs, scripts, tokens, secrets, or remote server settings in the package.

The deck name may be any Anki deck name, including subdecks such as:

```text
English::Business
```

## Output file

Create the actual downloadable file rather than only showing JSON in chat.

Name the file using a filesystem-safe version of the deck name followed by `.ankiimport`.

Examples:

```text
English.ankiimport
Business English.ankiimport
English - Business.ankiimport
```

If the environment provides a user-visible Python/file-generation tool, use it to write the JSON as UTF-8 and return a download link.

## User-facing response

Keep the response short. Report:

- selected deck;
- number of valid unique entries found;
- number of request-level duplicates discarded, if any;
- invalid lines, if any;
- download link for the `.ankiimport` file.

Then remind the user only that the Anki Desktop must be open before double-clicking the file.

Do not require the user to understand JSON or manually edit the package.

## Local Anki behavior

The `.ankiimport` package does not connect directly to the user's computer from ChatGPT.

The installed Windows Anki Importer handles the local Anki collection through AnkiConnect and AwesomeTTS. Its expected local rules are:

- card does not exist: create it and add English audio;
- card exists and already has audio: ignore it;
- card exists but has no audio: do not create a duplicate card; add only the missing audio to the existing Back field.

Do not claim these collection-level checks were performed during package generation.

## Final checks

Before returning the file, verify:

- the deck is present and non-empty;
- every card has both `front` and `back`;
- no duplicate Front values remain in the generated package;
- JSON is valid UTF-8;
- extension is exactly `.ankiimport`;
- `tts` is enabled unless explicitly disabled by the user.
