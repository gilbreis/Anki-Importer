import { createServer as createHttpServer } from "node:http";
import { createMcpHandler, McpServer } from "@modelcontextprotocol/server";
import { toNodeHandler } from "@modelcontextprotocol/node";
import * as z from "zod/v4";
import { DeviceHub } from "./DeviceHub.js";
import { DeviceCredentialStore } from "./DeviceCredentialStore.js";
import { PairingStore } from "./PairingStore.js";

const cardSchema = z.object({
  front: z.string().min(1).describe("Portuguese text placed in the Anki Front field"),
  back: z.string().min(1).describe("English translation placed in the Anki Back field"),
  tags: z.array(z.string().min(1)).optional(),
});

const importSchema = z.object({
  deck: z.string().min(1).describe("Exact destination deck name in Anki"),
  model: z.string().min(1).default("Basic"),
  frontField: z.string().min(1).default("Front"),
  backField: z.string().min(1).default("Back"),
  cards: z.array(cardSchema).min(1),
});

function textResult(value: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(value, null, 2) }],
    structuredContent: typeof value === "object" && value !== null
      ? value as Record<string, unknown>
      : { value },
  };
}

const credentials = new DeviceCredentialStore();
const pairing = new PairingStore();
let activeDeviceId: string | null = null;

const hub = new DeviceHub((deviceId, token) => credentials.validate(deviceId, token));

function requireActiveDeviceId(): string {
  if (!activeDeviceId) throw new Error("No Anki Desktop Companion is paired yet.");
  return activeDeviceId;
}

function createAnkiMcpServer(): McpServer {
  const server = new McpServer({
    name: "anki-vocabulary-importer",
    version: "0.2.0",
  });

  server.registerTool(
    "claim_anki_pairing",
    {
      title: "Pair Anki Desktop Companion",
      description: "Claim a short pairing code displayed by the Anki Desktop Companion. Use this once during setup.",
      inputSchema: z.object({ pairingCode: z.string().min(4).max(16) }),
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async ({ pairingCode }) => {
      const session = pairing.claim(pairingCode);
      if (!session?.deviceId || !session.deviceToken) {
        throw new Error("Pairing code is invalid, expired, or already used.");
      }

      credentials.register(session.deviceId, session.deviceToken);
      activeDeviceId = session.deviceId;

      return textResult({
        paired: true,
        deviceId: session.deviceId,
      });
    },
  );

  server.registerTool(
    "anki_health",
    {
      title: "Check Anki Desktop connection",
      description: "Check whether the paired Anki Desktop Companion and AnkiConnect are online. This tool is read-only.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => {
      const deviceId = requireActiveDeviceId();
      if (!hub.isOnline(deviceId)) return textResult({ ok: false, deviceOnline: false });
      return textResult(await hub.call(deviceId, "health"));
    },
  );

  server.registerTool(
    "list_anki_decks",
    {
      title: "List Anki decks",
      description: "List decks available in the paired Anki Desktop. Use this before importing when the requested deck name is uncertain.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => textResult(await hub.call(requireActiveDeviceId(), "list-decks")),
  );

  server.registerTool(
    "find_vocabulary_duplicates",
    {
      title: "Find vocabulary duplicates",
      description: "Check which Front values from a vocabulary batch already exist in the selected Anki deck. This never creates or modifies cards.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async input => textResult(await hub.call(requireActiveDeviceId(), "find-duplicates", input)),
  );

  server.registerTool(
    "add_vocabulary_cards",
    {
      title: "Add vocabulary cards",
      description: "Add only new Portuguese-to-English vocabulary cards to the selected Anki deck. Existing Front values are skipped and existing cards are never overwritten.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async input => textResult(await hub.call(requireActiveDeviceId(), "add-cards", input)),
  );

  return server;
}

const mcpHandler = createMcpHandler(() => createAnkiMcpServer());
const nodeMcpHandler = toNodeHandler(mcpHandler, {
  onerror(error) {
    console.error("MCP adapter error", error);
  },
});

const port = Number(process.env.PORT ?? "3000");

const httpServer = createHttpServer(async (req, res) => {
  const url = new URL(req.url ?? "/", `http://${req.headers.host ?? "localhost"}`);

  if (url.pathname === "/healthz") {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({
      ok: true,
      activeDeviceId,
      deviceOnline: activeDeviceId ? hub.isOnline(activeDeviceId) : false,
    }));
    return;
  }

  if (req.method === "POST" && url.pathname === "/pair/start") {
    const session = pairing.create();
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({
      pairingId: session.pairingId,
      pairingCode: session.pairingCode,
      expiresAt: new Date(session.expiresAt).toISOString(),
    }));
    return;
  }

  if (req.method === "GET" && url.pathname === "/pair/status") {
    const pairingId = url.searchParams.get("pairingId")?.trim();
    if (!pairingId) {
      res.writeHead(400, { "content-type": "application/json" });
      res.end(JSON.stringify({ error: "pairing_id_required" }));
      return;
    }

    const session = pairing.status(pairingId);
    if (!session) {
      res.writeHead(404, { "content-type": "application/json" });
      res.end(JSON.stringify({ error: "pairing_not_found_or_expired" }));
      return;
    }

    if (!session.claimed) {
      res.writeHead(200, { "content-type": "application/json" });
      res.end(JSON.stringify({ paired: false }));
      return;
    }

    const creds = pairing.consumeCredentials(pairingId);
    if (!creds) {
      res.writeHead(409, { "content-type": "application/json" });
      res.end(JSON.stringify({ error: "pairing_credentials_unavailable" }));
      return;
    }

    credentials.register(creds.deviceId, creds.deviceToken);
    activeDeviceId = creds.deviceId;

    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({
      paired: true,
      deviceId: creds.deviceId,
      deviceToken: creds.deviceToken,
    }));
    return;
  }

  if (url.pathname === "/mcp") {
    await nodeMcpHandler(req, res);
    return;
  }

  res.writeHead(404, { "content-type": "application/json" });
  res.end(JSON.stringify({ error: "not_found" }));
});

httpServer.on("upgrade", (req, socket, head) => {
  if (!hub.handleUpgrade(req, socket, head)) socket.destroy();
});

httpServer.listen(port, () => {
  console.error(`Anki Importer MCP server listening on port ${port}`);
  console.error("MCP endpoint: /mcp");
  console.error("Desktop Companion pairing endpoint: /pair/start");
});
