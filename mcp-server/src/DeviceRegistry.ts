import { mkdir, readFile, rename, writeFile } from "node:fs/promises";
import { dirname } from "node:path";

export interface RegisteredDevice {
  deviceId: string;
  token: string;
  displayName?: string;
  createdAt: string;
  lastSeenAt?: string;
}

interface RegistryFile {
  accounts: Record<string, {
    activeDeviceId?: string;
    devices: RegisteredDevice[];
  }>;
}

export class DeviceRegistry {
  private data: RegistryFile = { accounts: {} };
  private loaded = false;

  constructor(private readonly filePath: string) {}

  async ensureLoaded(): Promise<void> {
    if (this.loaded) return;

    try {
      const text = await readFile(this.filePath, "utf8");
      this.data = JSON.parse(text) as RegistryFile;
      if (!this.data.accounts) this.data.accounts = {};
    } catch (error: unknown) {
      const code = typeof error === "object" && error && "code" in error ? String((error as { code?: unknown }).code) : "";
      if (code !== "ENOENT") throw error;
      this.data = { accounts: {} };
    }

    this.loaded = true;
  }

  async registerDevice(accountId: string, device: RegisteredDevice): Promise<void> {
    await this.ensureLoaded();
    const account = this.data.accounts[accountId] ?? { devices: [] };
    const existingIndex = account.devices.findIndex(item => item.deviceId === device.deviceId);

    if (existingIndex >= 0) account.devices[existingIndex] = device;
    else account.devices.push(device);

    account.activeDeviceId = device.deviceId;
    this.data.accounts[accountId] = account;
    await this.save();
  }

  async validateDevice(deviceId: string, token: string): Promise<boolean> {
    await this.ensureLoaded();
    for (const account of Object.values(this.data.accounts)) {
      const device = account.devices.find(item => item.deviceId === deviceId);
      if (device && device.token === token) return true;
    }
    return false;
  }

  async getActiveDeviceId(accountId: string): Promise<string | null> {
    await this.ensureLoaded();
    return this.data.accounts[accountId]?.activeDeviceId ?? null;
  }

  async listDevices(accountId: string): Promise<Array<Omit<RegisteredDevice, "token"> & { active: boolean }>> {
    await this.ensureLoaded();
    const account = this.data.accounts[accountId];
    if (!account) return [];

    return account.devices.map(({ token: _token, ...device }) => ({
      ...device,
      active: account.activeDeviceId === device.deviceId,
    }));
  }

  async setActiveDevice(accountId: string, deviceId: string): Promise<void> {
    await this.ensureLoaded();
    const account = this.data.accounts[accountId];
    if (!account || !account.devices.some(item => item.deviceId === deviceId)) {
      throw new Error(`Device '${deviceId}' is not registered to account '${accountId}'.`);
    }

    account.activeDeviceId = deviceId;
    await this.save();
  }

  async removeDevice(accountId: string, deviceId: string): Promise<boolean> {
    await this.ensureLoaded();
    const account = this.data.accounts[accountId];
    if (!account) return false;

    const before = account.devices.length;
    account.devices = account.devices.filter(item => item.deviceId !== deviceId);
    if (account.devices.length === before) return false;

    if (account.activeDeviceId === deviceId) {
      account.activeDeviceId = account.devices[0]?.deviceId;
    }

    if (account.devices.length === 0) delete this.data.accounts[accountId];
    else this.data.accounts[accountId] = account;

    await this.save();
    return true;
  }

  async touch(deviceId: string): Promise<void> {
    await this.ensureLoaded();
    for (const account of Object.values(this.data.accounts)) {
      const device = account.devices.find(item => item.deviceId === deviceId);
      if (device) {
        device.lastSeenAt = new Date().toISOString();
        await this.save();
        return;
      }
    }
  }

  private async save(): Promise<void> {
    await mkdir(dirname(this.filePath), { recursive: true });
    const temp = `${this.filePath}.tmp`;
    await writeFile(temp, JSON.stringify(this.data, null, 2), "utf8");
    await rename(temp, this.filePath);
  }
}
