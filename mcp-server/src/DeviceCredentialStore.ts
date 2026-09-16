export class DeviceCredentialStore {
  private readonly tokens = new Map<string, string>();

  register(deviceId: string, token: string): void {
    this.tokens.set(deviceId, token);
  }

  validate(deviceId: string, token?: string): boolean {
    if (!token) return false;
    return this.tokens.get(deviceId) === token;
  }
}
