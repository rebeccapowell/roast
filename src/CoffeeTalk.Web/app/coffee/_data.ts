import 'server-only';

function getApiBaseUrl() {
  const baseUrl = process.env.API_URL;
  if (!baseUrl) {
    throw new Error('API_URL is not configured');
  }

  return baseUrl.replace(/\/$/, '');
}

export type CoffeeBarListItem = {
  id: string;
  name: string;
};

export async function getCoffeeBars(): Promise<CoffeeBarListItem[]> {
  const res = await fetch(`${getApiBaseUrl()}/coffee-bars`, {
    next: { tags: ['coffee-bar-list'] },
  });

  if (!res.ok) {
    throw new Error('Failed to load coffee bars');
  }

  return res.json() as Promise<CoffeeBarListItem[]>;
}
