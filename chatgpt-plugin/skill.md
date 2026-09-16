# Anki Vocabulary Importer

## Purpose

Import Portuguese-to-English vocabulary from user-provided text files into the user's paired Anki Desktop through the Anki Importer app.

## Invocation

Typical explicit invocation:

```text
@anki vocabulary importer Deck "English"
```

The user may attach a `.txt` file containing chat transcripts, timestamps, names, blank lines, and vocabulary entries.

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

Ignore transcript metadata such as timestamps, sender names, headers, and blank lines unless they are part of a valid mapping.

## Card mapping

- Front: Portuguese text from the left side of `=`.
- Back: English text from the right side of `=`.
- Default note model: `Basic`.
- Default fields: `Front` and `Back`.
- Destination deck: exact deck requested by the user.

Do not silently switch to a different deck when an exact deck name was supplied.

## Normalization

Before sending cards:

1. Trim leading/trailing whitespace.
2. Collapse repeated internal whitespace.
3. Preserve Portuguese accents and punctuation.
4. Preserve the English translation as supplied unless there is an obvious formatting-only issue.
5. Detect duplicate Front values inside the uploaded batch case-insensitively.

Do not invent translations for missing values. Mark malformed entries as invalid instead.

## Required workflow

### 1. Verify Anki connection

Call `anki_health` before a write operation.

If no device is paired, explain that the user must run the Desktop Companion pairing flow. When the user provides the short pairing code, call `claim_anki_pairing`.

If multiple devices exist and there is no clear active device, call `list_anki_devices` and let the user choose. Use `select_anki_device` only after the intended device is clear.

### 2. Resolve the deck

When the user supplied an exact deck name, use it exactly.

If the deck name is missing or uncertain, call `list_anki_decks` and ask/select based on the user's instruction. Never guess a similarly named deck when multiple matches exist.

### 3. Check duplicates

Call `find_vocabulary_duplicates` with the complete validated batch before writing.

Duplicate Front values already present in the selected deck must not be overwritten.

### 4. Add cards

Call `add_vocabulary_cards` with the validated batch. The backend is responsible for rechecking duplicates atomically before creation.

### 5. Report outcome

Return a compact report containing:

- destination deck;
- number found in the source;
- number added;
- duplicates skipped;
- invalid entries;
- errors.

When useful, list the added and skipped words.

## Safety and data handling

- Never expose or ask the user to paste the Companion device token.
- Never instruct the user to expose AnkiConnect port `8765` to the internet.
- AnkiConnect must remain local at `127.0.0.1:8765`.
- Pairing codes are short-lived setup credentials, not permanent secrets.
- Existing cards must not be modified or deleted by the vocabulary import workflow.

## Future audio behavior

Audio/TTS is not part of the current MVP. When implemented, generated English pronunciation audio should be attached to the Back side without changing the Front/Back vocabulary mapping or duplicate rules.
