import type { IncomingMessage } from "node:http";

interface TokenMap {
  [token: string]: string;
}

export class AccountAuth {
  private readonly tokenMap: TokenMap;
  private readonly devAccountId?: string;

  constructor() {
    this.devAccountId = process.env.ANKI_DEV_ACCOUNT_ID?.trim() || undefined;

    const raw = process.env.ANKI_ACCOUNT_TOKENS?.trim();
    if (!raw) {
      this.tokenMap = {};
      return;
    }

    const parsed = JSON.parse(raw) as unknown;
    if (!parsed || typeof parsed !== "object" || Array.isArray(parsed)) {
      throw new Error("ANKI_ACCOUNT_TOKENS must be a JSON object mapping bearer tokens to account IDs.");
    }

    this.tokenMap = parsed as TokenMap;
  }

  resolve(req: IncomingMessage): string | null {
    const authorization = req.headers.authorization?.trim();
    if (authorization?.toLowerCase().startsWith("bearer ")) {
      const token = authorization.slice(7).trim();
      const accountId = this.tokenMap[token];
      if (accountId) return accountId;
    }

    if (this.devAccountId) return this.devAccountId;
    return null;
  }
}
