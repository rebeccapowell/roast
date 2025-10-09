'use client';

import { useTransition } from 'react';
import { revealResults } from './actions';

export default function RevealClient({
  barId,
  currentStatus,
}: {
  barId: string;
  currentStatus?: string;
}) {
  const [pending, start] = useTransition();

  if (currentStatus === 'revealed') {
    return null;
  }

  return (
    <form action={(formData) => start(() => revealResults(formData))}>
      <input type="hidden" name="barId" value={barId} />
      <button type="submit" disabled={pending}>
        {pending ? 'Revealing…' : 'Reveal Results'}
      </button>
    </form>
  );
}
