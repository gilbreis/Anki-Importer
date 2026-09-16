import { AsyncLocalStorage } from "node:async_hooks";

export interface AccountContextValue {
  accountId: string;
}

const storage = new AsyncLocalStorage<AccountContextValue>();

export function runWithAccount<T>(accountId: string, fn: () => T): T {
  return storage.run({ accountId }, fn);
}

export function getCurrentAccountId(): string {
  const value = storage.getStore();
  if (!value) throw new Error("No authenticated Anki account context is available for this request.");
  return value.accountId;
}
