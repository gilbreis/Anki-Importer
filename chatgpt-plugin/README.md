# ChatGPT Plugin / App

ChatGPT-facing layer for Anki Importer.

## Intended user experience

User attaches a `.txt` file and asks:

```text
@anki vocabulary importer Deck "English"
```

The integration should:
1. Parse lines in `Portuguese = English` format.
2. Ignore timestamps/chat metadata around valid vocabulary lines.
3. Normalize whitespace.
4. Remove duplicates inside the uploaded file.
5. Ask the MCP service for real duplicates in the destination deck.
6. Add only new cards.
7. Return a compact import report.

## Default mapping

```text
Portuguese -> Front
English    -> Back
Model      -> Basic
```

These defaults may become configurable later.

## MVP response example

```text
Deck: English
Found: 18
Added: 15
Duplicates: 3
Invalid: 0
Errors: 0
```

## Future

- TTS/audio on Back
- Language detection
- Preview/approval mode
- User preferences for model and fields
