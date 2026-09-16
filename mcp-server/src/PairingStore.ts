import { randomBytes, randomUUID } from "node:crypto";

interface PairingSession {
  pairingCode: string;
  deviceId: string;
  deviceToken: string;
  createdAt: number;
  expiresAt: number;
}

export class PairingStore {
  private readonly sessions = new Map<string, PairingSession>();
  private readonly ttlMs: number;

  constructor(ttlMs = 10 * 60 * 1000) {
    this.ttlMs = ttlMs;
  }

  create(): PairingSession {
    this.cleanup();

    const pairingCode = this.generatePairingCode();
    const deviceId = randomUUID();
    const deviceToken = randomBytes(32).toString("base64url");
    const now = Date.now();

    const session: PairingSession = {
      pairingCode,
      deviceId,
      deviceToken,
      createdAt: now,
      expiresAt: now + this.ttlMs,
    };

    this.sessions.set(pairingCode, session);
    return session;
  }

  consume(pairingCode: string): PairingSession | null {
    this.cleanup();

    const normalized = pairingCode.trim().toUpperCase();
    const session = this.sessions.get(normalized) ?? null;
    if (!session) return null;

    this.sessions.delete(normalized);
    return session;
  }

  peek(pairingCode: string): PairingSession | null {
    this.cleanup();
    return this.sessions.get(pairingCode.trim().toUpperCase()) ?? null;
  }

  private cleanup(): void {
    const now = Date.now();
    for (const [code, session] of this.sessions.entries()) {
      if (session.expiresAt <= now) this.sessions.delete(code);
    }
  }

  private generatePairingCode(): string {
    const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    while (true) {
      const bytes = randomBytes(6);
      let code = "";
      for (const byte of bytes) code += alphabet[byte % alphabet.length];

      if (!this.sessions.has(code)) return code;
    }
  }
}
