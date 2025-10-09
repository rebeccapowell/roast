import { getCoffeeBar } from './_data';
import SubmitClient from './SubmitClient';
import VoteClient from './VoteClient';
import RevealClient from './RevealClient';

export default async function CoffeeBarPage({
  params,
}: {
  params: Promise<{ barId: string }>;
}) {
  const { barId } = await params;
  const bar = await getCoffeeBar(barId);

  return (
    <main>
      <h1>{bar.name}</h1>
      <p>Participants: {bar.participants}</p>

      <section>
        <h2>Submissions</h2>
        <ul>
          {bar.submissions.map((submission) => (
            <li key={submission.id}>
              <a href={submission.url} target="_blank" rel="noreferrer">
                {submission.title || submission.url}
              </a>
            </li>
          ))}
        </ul>
      </section>

      <section>
        <h2>Vote</h2>
        <VoteClient barId={bar.id} submissions={bar.submissions} />
      </section>

      <section>
        <h2>Submit a link</h2>
        <SubmitClient barId={bar.id} />
      </section>

      {bar.status === 'revealed' && (
        <section>
          <h2>Results</h2>
          <ul>
            {(bar.votes || []).map((vote) => (
              <li key={vote.submissionId}>
                {vote.submissionId}: {vote.count}
              </li>
            ))}
          </ul>
        </section>
      )}

      <RevealClient barId={bar.id} currentStatus={bar.status} />
    </main>
  );
}
