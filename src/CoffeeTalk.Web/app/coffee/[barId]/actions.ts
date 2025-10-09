'use server';

import { revalidateTag } from 'next/cache';

function getApiBaseUrl() {
  const baseUrl = process.env.API_URL;
  if (!baseUrl) {
    throw new Error('API_URL is not configured');
  }

  return baseUrl.replace(/\/$/, '');
}

function apiHeaders() {
  const headers: Record<string, string> = { 'content-type': 'application/json' };
  if (process.env.API_TOKEN) {
    headers.authorization = `Bearer ${process.env.API_TOKEN}`;
  }

  return headers;
}

export async function submitVote(formData: FormData) {
  const barId = String(formData.get('barId'));
  const submissionId = String(formData.get('submissionId'));

  const res = await fetch(`${getApiBaseUrl()}/coffee-bars/${barId}/vote`, {
    method: 'POST',
    headers: apiHeaders(),
    body: JSON.stringify({ submissionId }),
    cache: 'no-store',
  });

  if (!res.ok) {
    throw new Error('Vote failed');
  }

  revalidateTag(`coffee-bar:${barId}`);
}

export async function submitLink(formData: FormData) {
  const barId = String(formData.get('barId'));
  const url = String(formData.get('url'));

  const res = await fetch(`${getApiBaseUrl()}/coffee-bars/${barId}/submissions`, {
    method: 'POST',
    headers: apiHeaders(),
    body: JSON.stringify({ url }),
    cache: 'no-store',
  });

  if (!res.ok) {
    throw new Error('Submit failed');
  }

  revalidateTag(`coffee-bar:${barId}`);
}

export async function revealResults(formData: FormData) {
  const barId = String(formData.get('barId'));

  const res = await fetch(`${getApiBaseUrl()}/coffee-bars/${barId}/reveal`, {
    method: 'POST',
    headers: apiHeaders(),
    cache: 'no-store',
  });

  if (!res.ok) {
    throw new Error('Reveal failed');
  }

  revalidateTag(`coffee-bar:${barId}`);
}
