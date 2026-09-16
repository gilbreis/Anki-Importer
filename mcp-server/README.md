# MCP / Relay Server

Remote service used by the ChatGPT integration to reach the correct paired Anki Desktop Companion.

## MVP tools

### `health`
Returns whether the paired desktop companion is online and whether AnkiConnect is reachable.

### `list_decks`
Returns deck names from the user's Anki Desktop.

### `find_duplicates`
Input:
- deck
- Front values

Output:
- existing normalized Front values
- missing Front values

### `add_cards`
Input follows `shared/schemas/import-request.schema.json`.

Output should contain:
- total
- added
- duplicates
- invalid
- errors
- note IDs for successfully created notes

## Transport

The desktop companion should initiate the connection to the relay. The relay must not require public inbound access to the user's Windows machine.

## Pairing

Planned flow:
1. ChatGPT app requests a short-lived pairing code.
2. User enters/confirms the code in the Desktop Companion.
3. Relay binds the companion connection to the user's integration identity.
4. Tokens are rotated/revocable.
