const STORAGE_KEY = "coffee-talk:identities";

export type HipsterIdentity = {
  coffeeBarId: string;
  coffeeBarCode: string;
  hipsterId: string;
  username: string;
};

type IdentityDictionary = Record<string, HipsterIdentity>;

function readAll(): IdentityDictionary {
  if (typeof window === "undefined") {
    return {};
  }

  const raw = window.localStorage.getItem(STORAGE_KEY);
  if (!raw) {
    return {};
  }

  try {
    const parsed = JSON.parse(raw) as IdentityDictionary;
    return parsed ?? {};
  } catch {
    return {};
  }
}

function writeAll(values: IdentityDictionary) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(values));
}

export function saveIdentity(identity: HipsterIdentity) {
  const existing = readAll();
  existing[identity.coffeeBarCode.toUpperCase()] = identity;
  writeAll(existing);
}

export function getIdentity(coffeeBarCode: string): HipsterIdentity | undefined {
  const existing = readAll();
  return existing[coffeeBarCode.toUpperCase()];
}

export function removeIdentity(coffeeBarCode: string) {
  const existing = readAll();
  const normalized = coffeeBarCode.toUpperCase();
  if (existing[normalized]) {
    delete existing[normalized];
    writeAll(existing);
  }
}
