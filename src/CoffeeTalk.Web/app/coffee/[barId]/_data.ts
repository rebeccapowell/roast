import 'server-only';

function getApiBaseUrl() {
  const baseUrl = process.env.API_URL;
  if (!baseUrl) {
    throw new Error('API_URL is not configured');
  }

  return baseUrl.replace(/\/$/, '');
}

type Submission = { id: string; title?: string; url: string; owner?: string };
type Vote = { submissionId: string; count: number };
type BarStatus = 'open' | 'revealed';

type Bar = {
  id: string;
  name: string;
  participants: number;
  submissions: Submission[];
  votes?: Vote[];
  status?: BarStatus;
};

export type CoffeeBarDetail = Bar;

export async function getCoffeeBar(barId: string): Promise<CoffeeBarDetail> {
  const res = await fetch(`${getApiBaseUrl()}/coffee-bars/${barId}`, {
    next: { tags: [`coffee-bar:${barId}`] },
  });

  if (!res.ok) {
    throw new Error('Failed to load coffee bar');
  }

  return res.json() as Promise<CoffeeBarDetail>;
}
