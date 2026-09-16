import { randomUUID } from "node:crypto";
import type { IncomingMessage } from "node:http";
import type { Duplex } from "node:stream";
import { WebSocketServer, WebSocket } from "ws";

interface PendingRequest {
  resolve: (value: unknown) => void;
  reject: (error: Error) => void;
  timer: NodeJS.Timeout;
}

interface DeviceMessage {
  type: "result";
  id: string;
  ok: boolean;
  result?: unknown;
  error?: string;
}

export class DeviceHub {
  private readonly devices = new Map<string, WebSocket>();
  private readonly pending = new Map<string, PendingRequest>();
  private readonly wss = new WebSocketServer({ noServer: true });

  constructor(private readonly authenticate: (deviceId: string, token: string) => Promise<boolean>) {}

  async handleUpgrade(req: IncomingMessage, socket: Duplex, head: Buffer): Promise<boolean> {
    const url = new URL(req.url ?? "/", `http://${req.headers.host ?? "localhost"}`);
    if (url.pathname !== "/agent") return false;

    const deviceId = url.searchParams.get("deviceId")?.trim();
    const token = url.searchParams.get("token") ?? "";

    if (!deviceId || !token || !await this.authenticate(deviceId, token)) {
      socket.write("HTTP/1.1 401 Unauthorized\r\nConnection: close\r\n\r\n");
      socket.destroy();
      return true;
    }

    this.wss.handleUpgrade(req, socket, head, ws => {
      const previous = this.devices.get(deviceId);
      if (previous && previous.readyState === WebSocket.OPEN) previous.close(4001, "Replaced by a newer connection");

      this.devices.set(deviceId, ws);

      ws.on("message", data => this.onMessage(data.toString()));
      ws.on("close", () => {
        if (this.devices.get(deviceId) === ws) this.devices.delete(deviceId);
      });
      ws.on("error", () => {
        if (this.devices.get(deviceId) === ws) this.devices.delete(deviceId);
      });

      ws.send(JSON.stringify({ type: "connected", deviceId }));
    });

    return true;
  }

  isOnline(deviceId: string): boolean {
    return this.devices.get(deviceId)?.readyState === WebSocket.OPEN;
  }

  disconnect(deviceId: string, reason = "Device access revoked"): void {
    const ws = this.devices.get(deviceId);
    if (!ws) return;

    this.devices.delete(deviceId);
    if (ws.readyState === WebSocket.OPEN || ws.readyState === WebSocket.CONNECTING) {
      ws.close(4003, reason);
    }
  }

  async call(deviceId: string, action: string, payload?: unknown, timeoutMs = 15000): Promise<unknown> {
    const ws = this.devices.get(deviceId);
    if (!ws || ws.readyState !== WebSocket.OPEN) {
      throw new Error(`Anki device '${deviceId}' is offline.`);
    }

    const id = randomUUID();

    return await new Promise<unknown>((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(`Timed out waiting for device '${deviceId}' to execute '${action}'.`));
      }, timeoutMs);

      this.pending.set(id, { resolve, reject, timer });

      ws.send(JSON.stringify({
        type: "command",
        id,
        action,
        payload: payload ?? null,
      }), error => {
        if (!error) return;
        clearTimeout(timer);
        this.pending.delete(id);
        reject(error);
      });
    });
  }

  private onMessage(raw: string): void {
    let message: DeviceMessage;
    try {
      message = JSON.parse(raw) as DeviceMessage;
    } catch {
      return;
    }

    if (message.type !== "result" || !message.id) return;

    const item = this.pending.get(message.id);
    if (!item) return;

    clearTimeout(item.timer);
    this.pending.delete(message.id);

    if (message.ok) item.resolve(message.result);
    else item.reject(new Error(message.error || "Unknown device error."));
  }
}
