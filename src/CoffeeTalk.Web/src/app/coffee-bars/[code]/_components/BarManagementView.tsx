import { type HipsterIdentity } from "@/lib/identity";
import sharedStyles from "@/app/coffee-bars/[code]/CoffeeBarShared.module.css";
import styles from "@/app/coffee-bars/[code]/_components/BarManagementView.module.css";
import type {
  CoffeeBarResource,
  IngredientResource,
  SubmissionResource,
  SessionStateResource,
} from "@/app/coffee-bars/[code]/types";
import type { UseCoffeeBarClientResult } from "@/app/coffee-bars/[code]/useCoffeeBarClient";

type SubmissionSummary = {
  submission: SubmissionResource;
  ingredient?: IngredientResource;
};

type BarManagementViewProps = {
  coffeeBar: CoffeeBarResource;
  identity: HipsterIdentity | null;
  hasIdentity: boolean;
  sessionState: SessionStateResource | null;
  submissionCounts: Record<string, number>;
  availableIngredients: number;
  join: UseCoffeeBarClientResult["join"];
  submission: UseCoffeeBarClientResult["submission"];
  session: UseCoffeeBarClientResult["session"];
  mySubmissions: SubmissionSummary[];
  voteError: string | null;
  onOpenCycleView: () => void;
  submissionsLocked: boolean;
};

function formatSessionStatus(
  sessionState: SessionStateResource | null,
  hasActiveSession: boolean,
  endedAt: string | null
) {
  if (!sessionState) {
    return "No active session yet.";
  }

  if (hasActiveSession) {
    return `Session started ${new Date(
      sessionState.session.startedAt
    ).toLocaleTimeString()}`;
  }

  if (endedAt) {
    return `Session ended ${new Date(endedAt).toLocaleTimeString()}`;
  }

  return "Session ended recently";
}

export function BarManagementView({
  coffeeBar,
  identity,
  hasIdentity,
  sessionState,
  submissionCounts,
  availableIngredients,
  join,
  submission,
  session,
  mySubmissions,
  voteError,
  onOpenCycleView,
  submissionsLocked,
}: BarManagementViewProps) {
  const sessionStatus = formatSessionStatus(
    sessionState,
    session.hasActiveSession,
    session.endedAt
  );

  return (
    <div className={styles.layout}>
      <aside className={styles.sidebar}>
        <section className={sharedStyles.card}>
          <h2 className={sharedStyles.cardTitle}>Hipsters in the bar</h2>
          <ul className={styles.hipsterList}>
            {coffeeBar.hipsters.length === 0 && (
              <li>No hipsters yet. Be the first to join!</li>
            )}
            {coffeeBar.hipsters.map((hipster) => {
              const isMe = identity?.hipsterId === hipster.id;

              return (
                <li
                  key={hipster.id}
                  className={isMe ? sharedStyles.me : undefined}
                >
                  <span className={sharedStyles.hipsterName}>
                    {hipster.username}
                  </span>
                  <span className={sharedStyles.hipsterCount}>
                    {submissionCounts[hipster.id] ?? 0} urls
                  </span>
                </li>
              );
            })}
          </ul>
        </section>

        <section className={sharedStyles.card}>
          <h2 className={sharedStyles.cardTitle}>Session</h2>
          <p className={sharedStyles.sessionStatus}>{sessionStatus}</p>
          {session.hasActiveSession ? (
            <button
              className={sharedStyles.secondaryButton}
              type="button"
              onClick={session.end}
              disabled={
                session.ending || !hasIdentity || Boolean(session.activeCycle)
              }
            >
              {session.ending ? "Stopping…" : "Stop session"}
            </button>
          ) : (
            <button
              className={sharedStyles.primaryButton}
              type="button"
              onClick={session.start}
              disabled={
                session.loading || availableIngredients === 0 || !hasIdentity
              }
            >
              {session.loading
                ? "Starting…"
                : sessionState
                ? "Start new session"
                : "Start session"}
            </button>
          )}
          <p className={sharedStyles.sessionHint}>
            {availableIngredients === 0
              ? "Add more ingredients before brewing."
              : `${availableIngredients} ingredient${
                  availableIngredients === 1 ? "" : "s"
                } ready to brew.`}
          </p>
          {session.hasActiveSession && session.activeCycle && (
            <p className={sharedStyles.sessionHint}>
              Reveal the current video before stopping the session.
            </p>
          )}
          {!hasIdentity && (
            <p className={sharedStyles.sessionHint}>
              Join the bar to control the brew.
            </p>
          )}
        </section>

        {!hasIdentity && (
          <section className={sharedStyles.card}>
            <h2 className={sharedStyles.cardTitle}>Join this bar</h2>
            <form className={styles.joinForm} onSubmit={join.handleSubmit}>
              <input
                className={sharedStyles.input}
                value={join.username}
                onChange={(event) => join.setUsername(event.target.value)}
                placeholder="Your username"
                required
                minLength={3}
                maxLength={20}
                disabled={join.loading}
              />
              <button
                className={sharedStyles.primaryButton}
                type="submit"
                disabled={join.loading}
              >
                {join.loading ? "Joining…" : "Join"}
              </button>
            </form>
            {join.error && (
              <div className={sharedStyles.inlineError}>{join.error}</div>
            )}
          </section>
        )}

        {hasIdentity && identity && (
          <section className={sharedStyles.card}>
            <h2 className={sharedStyles.cardTitle}>You’re in</h2>
            <p className={styles.identityLine}>
              Signed in as <strong>{identity.username}</strong>
            </p>
          </section>
        )}
      </aside>

      <main className={styles.main}>
        {hasIdentity ? (
          <section className={sharedStyles.card}>
            <h2 className={sharedStyles.cardTitle}>Submit a new ingredient</h2>
            <form
              className={styles.submitForm}
              onSubmit={submission.handleSubmit}
            >
              <input
                className={sharedStyles.input}
                value={submission.url}
                onChange={(event) => submission.setUrl(event.target.value)}
                placeholder="Paste a YouTube URL"
                required
                disabled={submission.loading || submissionsLocked}
              />
              <button
                className={sharedStyles.primaryButton}
                type="submit"
                disabled={submission.loading || submissionsLocked}
              >
                {submission.loading ? "Submitting…" : "Submit"}
              </button>
            </form>
            {submissionsLocked && (
              <p className={sharedStyles.sessionHint}>
                Submissions are locked while brewing.
              </p>
            )}
          </section>
        ) : (
          <section className={sharedStyles.card}>
            <h2 className={sharedStyles.cardTitle}>Ready to brew?</h2>
            <p className={sharedStyles.sessionHint}>
              Join the bar from the left column to start submitting your
              favourite tracks.
            </p>
          </section>
        )}

        {hasIdentity && (
          <section className={sharedStyles.card}>
            <h2 className={sharedStyles.cardTitle}>Your submissions</h2>
            {mySubmissions.length === 0 ? (
              <p className={sharedStyles.sessionHint}>
                You haven’t queued any videos yet.
              </p>
            ) : (
              <ul className={styles.submissionList}>
                {mySubmissions.map(
                  ({ submission: mySubmission, ingredient }) => (
                    <li key={mySubmission.id}>
                      <div className={styles.submissionRow}>
                        <div className={styles.submissionInfo}>
                          {ingredient?.thumbnailUrl ? (
                            <>
                              {/* eslint-disable-next-line @next/next/no-img-element */}
                              <img
                                className={styles.submissionThumbnail}
                                src={ingredient.thumbnailUrl}
                                alt={ingredient?.title ?? "YouTube thumbnail"}
                              />
                            </>
                          ) : null}
                          <div className={styles.submissionText}>
                            <div className={styles.submissionTitle}>
                              {ingredient?.title ?? "Unknown video"}
                            </div>
                            {ingredient ? (
                              <a
                                className={sharedStyles.shareAnchor}
                                href={`https://youtu.be/${ingredient.videoId}`}
                                target="_blank"
                                rel="noreferrer"
                              >
                                https://youtu.be/{ingredient.videoId}
                              </a>
                            ) : null}
                          </div>
                        </div>
                        <button
                          type="button"
                          className={sharedStyles.secondaryButton}
                          onClick={() =>
                            submission.handleRemove(mySubmission.id)
                          }
                        >
                          Remove
                        </button>
                      </div>
                    </li>
                  )
                )}
              </ul>
            )}
          </section>
        )}

        <section className={sharedStyles.card}>
          <h2 className={sharedStyles.cardTitle}>Live cycle</h2>
          {sessionState ? (
            <>
              <p className={sharedStyles.sessionHint}>
                {session.statusMessage}
              </p>
              <button
                type="button"
                className={`${sharedStyles.primaryButton} ${styles.cycleCardAction}`}
                onClick={onOpenCycleView}
                disabled={!session.hasActiveSession && !session.latestCycle}
              >
                {session.hasActiveSession
                  ? "Open cycle view"
                  : "Review cycle view"}
              </button>
            </>
          ) : (
            <p className={sharedStyles.sessionHint}>
              Start a session to brew the first video.
            </p>
          )}
        </section>

        {voteError && (
          <div className={sharedStyles.inlineError}>{voteError}</div>
        )}
      </main>
    </div>
  );
}
