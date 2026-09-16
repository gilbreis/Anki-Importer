import { randomBytes, randomUUID } from "node:crypto";

export interface PairingSession {
  pairingId: string;
  pairingCode: string;
  createdAt: number;
  expiresAt: number;
  claimed: boolean;
  accountId?: string;
  deviceId?: string;
  deviceToken?: string;
}

interface LinkPairingSession {
  ticket: string;
  accountId: string;
  createdAt: number;
  expiresAt: number;
}

export class PairingStore {
  private readonly sessionsById = new Map<string, PairingSession>();
  private readonly idByCode = new Map<string, string>();
  private readonly linksByTicket = new Map<string, LinkPairingSession>();

  constructor(private readonly ttlMs = 10 * 60 * 1000) {}

  create(): PairingSession {
    this.cleanup();

    const pairingId = randomUUID();
    const pairingCode = this.generatePairingCode();
    const now = Date.now();

    const session: PairingSession = {
      pairingId,
      pairingCode,
      createdAt: now,
      expiresAt: now + this.ttlMs,
      claimed: false,
    };

    this.sessionsById.set(pairingId, session);
    this.idByCode.set(pairingCode, pairingId);
    return session;
  }

  createLink(accountId: string): LinkPairingSession {
    this.cleanup();
    const now = Date.now();
    const ticket = randomBytes(32).toString("base64url");
    const session: LinkPairingSession = {
      ticket,
      accountId,
      createdAt: now,
      expiresAt: now + this.ttlMs,
    };
    this.linksByTicket.set(ticket, session);
    return session;
  }

  activateLink(ticket: string): { accountId: string; deviceId: string; deviceToken: string } | null {
    this.cleanup();
    const session = this.linksByTicket.get(ticket);
    if (!session) return null;

    this.linksByTicket.delete(ticket);
    return {
      accountId: session.accountId,
      deviceId: randomUUID(),
      deviceToken: randomBytes(32).toString("base64url"),
    };
  }

  claim(pairingCode: string, accountId: string): PairingSession | null {
    this.cleanup();

    const normalized = pairingCode.trim().toUpperCase();
    const pairingId = this.idByCode.get(normalized);
    if (!pairingId) return null;

    const session = this.sessionsById.get(pairingId);
    if (!session || session.claimed) return null;

    session.claimed = true;
    session.accountId = accountId;
    session.deviceId = randomUUID();
    session.deviceToken = randomBytes(32).toString("base64url");
    this.idByCode.delete(normalized);

    return session;
  }

  status(pairingId: string): PairingSession | null {
    this.cleanup();
    return this.sessionsById.get(pairingId) ?? null;
  }

  consumeCredentials(pairingId: string): { accountId: string; deviceId: string; deviceToken: string } | null {
    this.cleanup();

    const session = this.sessionsById.get(pairingId);
    if (!session?.claimed || !session.accountId || !session.deviceId || !session.deviceToken) return null;

    this.sessionsById.delete(pairingId);
    return {
      accountId: session.accountId,
      deviceId: session.deviceId,
      deviceToken: session.deviceToken,
    };
  }

  private cleanup(): void {
    const now = Date.now();

    for (const [pairingId, session] of this.sessionsById.entries()) {
      if (session.expiresAt > now) continue;
      this.sessionsById.delete(pairingId);
      this.idByCode.delete(session.pairingCode);
    }

    for (const [ticket, session] of this.linksByTicket.entries()) {
      if (session.expiresAt <= now) this.linksByTicket.delete(ticket);
    }
  }

  private generatePairingCode(): string {
    const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    while (true) {
      const bytes = randomBytes(6);
      let code = "";
      for (const byte of bytes) code += alphabet[byte % alphabet.length];
      if (!this.idByCode.has(code)) return code;
    }
  }
}
