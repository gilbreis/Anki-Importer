# MCP / Relay Server

Remote service used by the ChatGPT integration to reach the authenticated user's paired Anki Desktop Companion.

## Current tools

- `claim_anki_pairing` — associates a Companion pairing code with the authenticated account.
- `list_anki_devices` — lists devices paired to that account.
- `select_anki_device` — changes the active device for that account.
- `anki_health` — checks the active Companion and AnkiConnect.
- `list_anki_decks` — returns deck names from the active Anki Desktop.
- `find_vocabulary_duplicates` — read-only duplicate lookup by normalized Front value.
- `add_vocabulary_cards` — adds only cards that are not already present.

## Multi-user isolation

Each MCP request is resolved to an `accountId`. The server then loads that account's active device from the persistent device registry. A user can never select a device that is not registered to their own account through the MCP tools.

The account resolver is deliberately isolated in `AccountAuth.ts` so the prototype bearer-token resolver can later be replaced by OAuth/app identity without changing the Anki or device-routing logic.

### Development mode

Set:

```bash
ANKI_DEV_ACCOUNT_ID=local-dev
```

All MCP requests resolve to that development account.

### Multi-user prototype mode

Remove `ANKI_DEV_ACCOUNT_ID` and define:

```bash
ANKI_ACCOUNT_TOKENS={"secret-token-a":"account-a","secret-token-b":"account-b"}
```

MCP requests must include:

```http
Authorization: Bearer secret-token-a
```

This token map is only a prototype authentication mechanism. Production should replace it with OAuth-backed identity.

## Pairing flow

1. Companion calls `POST /pair/start`.
2. Server returns a short-lived pairing code.
3. User gives the pairing code to the ChatGPT integration.
4. ChatGPT calls `claim_anki_pairing` under the authenticated account.
5. Server creates a unique `deviceId` and device token and stores the device under that account.
6. Companion polls `/pair/status`, receives the device credential once, and protects it locally with Windows DPAPI.
7. Companion opens an outbound authenticated WebSocket to `/agent`.

No inbound port is opened on the user's Windows PC. AnkiConnect remains bound to `127.0.0.1:8765`.

## Persistent registry

Default path:

```text
./data/device-registry.json
```

Override with:

```bash
ANKI_REGISTRY_PATH=/data/device-registry.json
```

The registry contains device bearer tokens. Keep it outside source control, restrict filesystem access, and persist the data volume when deploying the container.

## Local development

```bash
cd mcp-server
npm install
npm run typecheck
npm run dev
```

The MCP endpoint is:

```text
http://localhost:3000/mcp
```

Health probe:

```text
GET /healthz
```

## Docker

```bash
docker build -t anki-importer-mcp .
docker run --rm -p 3000:3000 \
  -e ANKI_DEV_ACCOUNT_ID=local-dev \
  -v anki-importer-data:/app/data \
  anki-importer-mcp
```

For production, terminate TLS in front of the service and expose the MCP endpoint over HTTPS.
