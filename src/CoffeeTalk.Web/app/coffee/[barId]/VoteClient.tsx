'use client';

import { useTransition } from 'react';
import { submitVote } from './actions';

export default function VoteClient({
  barId,
  submissions,
}: {
  barId: string;
  submissions: Array<{ id: string; title?: string; url: string }>;
}) {
  const [pending, start] = useTransition();

  return (
    <form action={(formData) => start(() => submitVote(formData))}>
      <input type="hidden" name="barId" value={barId} />
      <fieldset disabled={pending}>
        {submissions.map((submission) => (
          <button key={submission.id} type="submit" name="submissionId" value={submission.id}>
            Vote for {submission.title || submission.url}
          </button>
        ))}
      </fieldset>
      {pending && <p>Submitting vote…</p>}
    </form>
  );
}
