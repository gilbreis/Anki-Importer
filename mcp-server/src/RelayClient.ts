export interface VocabularyCard {
  front: string;
  back: string;
  tags?: string[];
}

export interface ImportRequest {
  deck: string;
  model?: string;
  frontField?: string;
  backField?: string;
  cards: VocabularyCard[];
}

export class RelayClient {
  constructor(
    private readonly baseUrl: string,
    private readonly deviceId: string,
    private readonly token?: string,
  ) {}

  async health(): Promise<unknown> {
    return this.request("GET", `/v1/devices/${encodeURIComponent(this.deviceId)}/health`);
  }

  async listDecks(): Promise<unknown> {
    return this.request("GET", `/v1/devices/${encodeURIComponent(this.deviceId)}/decks`);
  }

  async findDuplicates(payload: ImportRequest): Promise<unknown> {
    return this.request("POST", `/v1/devices/${encodeURIComponent(this.deviceId)}/find-duplicates`, payload);
  }

  async addCards(payload: ImportRequest): Promise<unknown> {
    return this.request("POST", `/v1/devices/${encodeURIComponent(this.deviceId)}/add-cards`, payload);
  }

  private async request(method: string, path: string, body?: unknown): Promise<unknown> {
    const headers = new Headers({ Accept: "application/json" });
    if (body !== undefined) headers.set("Content-Type", "application/json; charset=utf-8");
    if (this.token) headers.set("Authorization", `Bearer ${this.token}`);

    const response = await fetch(new URL(path, this.baseUrl), {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
    });

    const text = await response.text();
    let payload: unknown = text;
    if (text) {
      try {
        payload = JSON.parse(text);
      } catch {
        // Keep the raw response text for diagnostics.
      }
    }

    if (!response.ok) {
      throw new Error(`Relay returned HTTP ${response.status}: ${typeof payload === "string" ? payload : JSON.stringify(payload)}`);
    }

    return payload;
  }
}
