'use client';

import { useTransition } from 'react';
import { submitLink } from './actions';

export default function SubmitClient({ barId }: { barId: string }) {
  const [pending, start] = useTransition();

  return (
    <form action={(formData) => start(() => submitLink(formData))}>
      <input type="hidden" name="barId" value={barId} />
      <input name="url" placeholder="https://youtu.be/..." required />
      <button type="submit" disabled={pending}>
        Submit
      </button>
      {pending && <p>Submitting…</p>}
    </form>
  );
}
