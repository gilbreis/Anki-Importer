import { createServer as createHttpServer } from "node:http";
import { createMcpHandler, McpServer } from "@modelcontextprotocol/server";
import { toNodeHandler } from "@modelcontextprotocol/node";
import * as z from "zod/v4";
import { AccountAuth } from "./AccountAuth.js";
import { getCurrentAccountId, runWithAccount } from "./AccountContext.js";
import { DeviceHub } from "./DeviceHub.js";
import { DeviceRegistry } from "./DeviceRegistry.js";
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

function resolvePublicBaseUrl(): string | undefined {
  const explicit = process.env.ANKI_PUBLIC_BASE_URL?.trim();
  if (explicit) return explicit.replace(/\/$/, "");

  const renderUrl = process.env.RENDER_EXTERNAL_URL?.trim();
  if (renderUrl) return renderUrl.replace(/\/$/, "");

  const renderHost = process.env.RENDER_EXTERNAL_HOSTNAME?.trim();
  if (renderHost) return `https://${renderHost.replace(/\/$/, "")}`;

  return undefined;
}

const registryPath = process.env.ANKI_REGISTRY_PATH ?? "./data/device-registry.json";
const publicBaseUrl = resolvePublicBaseUrl();
const installerUrl = process.env.ANKI_INSTALLER_URL ??
  "https://github.com/gilbreis/Anki-Importer/releases/latest/download/AnkiImporterSetup.exe";
const registry = new DeviceRegistry(registryPath);
await registry.ensureLoaded();

const auth = new AccountAuth();
const pairing = new PairingStore();
const hub = new DeviceHub(async (deviceId, token) => registry.validateDevice(deviceId, token));

async function requireActiveDeviceId(): Promise<string> {
  const accountId = getCurrentAccountId();
  const deviceId = await registry.getActiveDeviceId(accountId);
  if (!deviceId) throw new Error("No Anki Desktop Companion is paired to this account yet.");
  return deviceId;
}

function createAnkiMcpServer(): McpServer {
  const server = new McpServer({
    name: "anki-vocabulary-importer",
    version: "0.4.0",
  });

  server.registerTool(
    "create_anki_connection_link",
    {
      title: "Connect this computer to Anki Importer",
      description: "Create a short-lived HTTPS link that opens the installed Anki Importer Companion and pairs it with the authenticated account. Prefer this over manual pairing codes.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async () => {
      if (!publicBaseUrl) throw new Error("One-click pairing is not configured on this server.");
      const session = pairing.createLink(getCurrentAccountId());
      return textResult({
        connectUrl: `${publicBaseUrl}/connect?ticket=${encodeURIComponent(session.ticket)}`,
        expiresAt: new Date(session.expiresAt).toISOString(),
      });
    },
  );

  server.registerTool(
    "claim_anki_pairing",
    {
      title: "Pair Anki Desktop Companion",
      description: "Fallback setup only: claim a short pairing code displayed by the Anki Desktop Companion.",
      inputSchema: z.object({ pairingCode: z.string().min(4).max(16) }),
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async ({ pairingCode }) => {
      const accountId = getCurrentAccountId();
      const session = pairing.claim(pairingCode, accountId);
      if (!session?.deviceId || !session.deviceToken) {
        throw new Error("Pairing code is invalid, expired, or already used.");
      }

      await registry.registerDevice(accountId, {
        deviceId: session.deviceId,
        token: session.deviceToken,
        createdAt: new Date().toISOString(),
      });

      return textResult({ paired: true, deviceId: session.deviceId });
    },
  );

  server.registerTool(
    "list_anki_devices",
    {
      title: "List paired Anki devices",
      description: "List Anki Desktop Companion devices paired with the authenticated account and show which one is active.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => textResult({ devices: await registry.listDevices(getCurrentAccountId()) }),
  );

  server.registerTool(
    "select_anki_device",
    {
      title: "Select active Anki device",
      description: "Choose which paired Anki Desktop Companion should receive subsequent Anki operations for this account.",
      inputSchema: z.object({ deviceId: z.string().uuid() }),
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async ({ deviceId }) => {
      await registry.setActiveDevice(getCurrentAccountId(), deviceId);
      return textResult({ selected: true, deviceId });
    },
  );

  server.registerTool(
    "revoke_anki_device",
    {
      title: "Revoke paired Anki device",
      description: "Permanently revoke a paired Anki Desktop Companion from the authenticated account. Its existing connection is closed and its device token becomes invalid.",
      inputSchema: z.object({ deviceId: z.string().uuid() }),
      annotations: { readOnlyHint: false, destructiveHint: true },
    },
    async ({ deviceId }) => {
      const removed = await registry.removeDevice(getCurrentAccountId(), deviceId);
      if (!removed) throw new Error("The specified device is not paired to this account.");
      hub.disconnect(deviceId, "Device access revoked");
      return textResult({ revoked: true, deviceId });
    },
  );

  server.registerTool(
    "anki_health",
    {
      title: "Check Anki Desktop connection",
      description: "Check whether the active paired Anki Desktop Companion and AnkiConnect are online. This tool is read-only.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => {
      const deviceId = await requireActiveDeviceId();
      if (!hub.isOnline(deviceId)) return textResult({ ok: false, deviceOnline: false, deviceId });
      await registry.touch(deviceId);
      return textResult(await hub.call(deviceId, "health"));
    },
  );

  server.registerTool(
    "list_anki_decks",
    {
      title: "List Anki decks",
      description: "List decks available in the active paired Anki Desktop. Use this before importing when the requested deck name is uncertain.",
      inputSchema: z.object({}),
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async () => {
      const deviceId = await requireActiveDeviceId();
      await registry.touch(deviceId);
      return textResult(await hub.call(deviceId, "list-decks"));
    },
  );

  server.registerTool(
    "find_vocabulary_duplicates",
    {
      title: "Find vocabulary duplicates",
      description: "Check which Front values from a vocabulary batch already exist in the selected Anki deck. This never creates or modifies cards.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: true, destructiveHint: false },
    },
    async input => {
      const deviceId = await requireActiveDeviceId();
      await registry.touch(deviceId);
      return textResult(await hub.call(deviceId, "find-duplicates", input));
    },
  );

  server.registerTool(
    "add_vocabulary_cards",
    {
      title: "Add vocabulary cards",
      description: "Add only new Portuguese-to-English vocabulary cards to the selected Anki deck. Existing Front values are skipped and existing cards are never overwritten.",
      inputSchema: importSchema,
      annotations: { readOnlyHint: false, destructiveHint: false },
    },
    async input => {
      const deviceId = await requireActiveDeviceId();
      await registry.touch(deviceId);
      return textResult(await hub.call(deviceId, "add-cards", input));
    },
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

function unauthorized(res: import("node:http").ServerResponse) {
  res.writeHead(401, { "content-type": "application/json", "www-authenticate": "Bearer" });
  res.end(JSON.stringify({ error: "unauthorized" }));
}

function html(res: import("node:http").ServerResponse, status: number, body: string) {
  res.writeHead(status, { "content-type": "text/html; charset=utf-8", "cache-control": "no-store" });
  res.end(body);
}

const httpServer = createHttpServer(async (req, res) => {
  const url = new URL(req.url ?? "/", `http://${req.headers.host ?? "localhost"}`);

  if (url.pathname === "/healthz") {
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ ok: true }));
    return;
  }

  if (req.method === "GET" && url.pathname === "/connect") {
    const ticket = url.searchParams.get("ticket")?.trim();
    if (!ticket || !publicBaseUrl) {
      html(res, 400, "<h1>Link de conexão inválido.</h1>");
      return;
    }

    const launch = `anki-importer://pair?server=${encodeURIComponent(publicBaseUrl)}&ticket=${encodeURIComponent(ticket)}`;
    html(res, 200, `<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Conectar Anki Importer</title></head><body style="font-family:Segoe UI,Arial,sans-serif;max-width:620px;margin:80px auto;padding:24px;text-align:center"><h1>Conectar Anki Importer</h1><p>Abra o Anki Importer instalado neste computador para concluir a conexão.</p><p><a style="display:inline-block;padding:14px 22px;background:#111;color:white;border-radius:8px;text-decoration:none" href="${launch}">Conectar este computador</a></p><p style="margin-top:30px;color:#666">Ainda não instalou?</p><p><a href="${installerUrl}">Instalar Anki Importer</a></p><p style="color:#777;font-size:13px">Depois da instalação, volte a esta página e clique em Conectar este computador.</p><script>setTimeout(()=>{location.href=${JSON.stringify(launch)}},350);</script></body></html>`);
    return;
  }

  if (req.method === "POST" && url.pathname === "/pair/link/activate") {
    let raw = "";
    for await (const chunk of req) raw += chunk;
    let ticket = "";
    try {
      const parsed = JSON.parse(raw || "{}") as { ticket?: string };
      ticket = parsed.ticket?.trim() ?? "";
    } catch {
      // handled below
    }

    const creds = ticket ? pairing.activateLink(ticket) : null;
    if (!creds) {
      res.writeHead(400, { "content-type": "application/json" });
      res.end(JSON.stringify({ error: "pairing_link_invalid_or_expired" }));
      return;
    }

    await registry.registerDevice(creds.accountId, {
      deviceId: creds.deviceId,
      token: creds.deviceToken,
      createdAt: new Date().toISOString(),
    });

    res.writeHead(200, { "content-type": "application/json", "cache-control": "no-store" });
    res.end(JSON.stringify({
      paired: true,
      deviceId: creds.deviceId,
      deviceToken: creds.deviceToken,
    }));
    return;
  }

  if (req.method === "POST" && url.pathname === "/pair/start") {
    const session = pairing.create();
    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ pairingId: session.pairingId, pairingCode: session.pairingCode, expiresAt: new Date(session.expiresAt).toISOString() }));
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

    res.writeHead(200, { "content-type": "application/json" });
    res.end(JSON.stringify({ paired: true, deviceId: creds.deviceId, deviceToken: creds.deviceToken }));
    return;
  }

  if (url.pathname === "/mcp") {
    const accountId = auth.resolve(req);
    if (!accountId) {
      unauthorized(res);
      return;
    }

    await runWithAccount(accountId, () => nodeMcpHandler(req, res));
    return;
  }

  res.writeHead(404, { "content-type": "application/json" });
  res.end(JSON.stringify({ error: "not_found" }));
});

httpServer.on("upgrade", async (req, socket, head) => {
  try {
    if (!await hub.handleUpgrade(req, socket, head)) socket.destroy();
  } catch (error) {
    console.error("WebSocket upgrade error", error);
    socket.destroy();
  }
});

httpServer.listen(port, () => {
  console.error(`Anki Importer MCP server listening on port ${port}`);
  console.error("MCP endpoint: /mcp");
  console.error("Desktop Companion pairing endpoint: /pair/start");
  console.error(`Device registry: ${registryPath}`);
  console.error(`Public base URL: ${publicBaseUrl ?? "not configured"}`);
});
