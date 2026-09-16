import { createServer as createHttpServer } from "node:http";
import { createMcpHandler, McpServer } from "@modelcontextprotocol/server";
import { toNodeHandler } from "@modelcontextprotocol/node";
import * as z from "zod/v4";
import { DeviceHub } from "./DeviceHub.js";

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

function createAnkiMcpServer(hub: DeviceHub, deviceId: string): McpServer {
  const server = new McpServer({
    name: "anki-vocabulary-importer",
    version: "0.1.0",
  });

  server.registerTool(
    "anki_health",
    {
      title: "Check Anki Desktop connection",
      description: "Check whether the paired Anki Desktop Companion and AnkiConnect are online. This tool is read-only.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => {
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
    async () => textResult(await hub.call(deviceId, "list-decks")),
  );

  server.registerTool(
    "find_vocabulary_duplicates",
    {
      title: "Find vocabulary duplicates",
      description: "Check which Front values from a vocabulary batch already exist in the selected Anki deck. This never creates or modifies cards.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async input => textResult(await hub.call(deviceId, "find-duplicates", input)),
  );

  server.registerTool(
    "add_vocabulary_cards",
    {
      title: "Add vocabulary cards",
      description: "Add only new Portuguese-to-English vocabulary cards to the selected Anki deck. Existing Front values are skipped and existing cards are never overwritten.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async input => textResult(await hub.call(deviceId, "add-cards", input)),
  );

  return server;
}

const deviceId = process.env.ANKI_DEVICE_ID ?? "development-device";
const agentToken = process.env.ANKI_AGENT_TOKEN;
const hub = new DeviceHub(agentToken);

const mcpHandler = createMcpHandler(() => createAnkiMcpServer(hub, deviceId));
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
    res.end(JSON.stringify({ ok: true, deviceId, deviceOnline: hub.isOnline(deviceId) }));
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
  console.error(`MCP endpoint: /mcp`);
  console.error(`Desktop Companion WebSocket endpoint: /agent?deviceId=${encodeURIComponent(deviceId)}`);
});
