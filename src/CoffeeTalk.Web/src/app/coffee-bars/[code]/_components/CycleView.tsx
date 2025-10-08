import { type HipsterIdentity } from "@/lib/identity";
import sharedStyles from "@/app/coffee-bars/[code]/CoffeeBarShared.module.css";
import styles from "@/app/coffee-bars/[code]/_components/CycleView.module.css";
import type {
  CoffeeBarResource,
  SessionStateResource,
} from "@/app/coffee-bars/[code]/types";
import type { UseCoffeeBarClientResult } from "@/app/coffee-bars/[code]/useCoffeeBarClient";
import { YouTubeEmbed } from "@/app/coffee-bars/[code]/_components/YouTubeEmbed";

type CycleViewProps = {
  coffeeBar: CoffeeBarResource;
  sessionState: SessionStateResource | null;
  identity: HipsterIdentity | null;
  hasIdentity: boolean;
  onBackToBar: () => void;
  session: UseCoffeeBarClientResult["session"];
  cycle: UseCoffeeBarClientResult["cycle"];
  availableIngredients: number;
  isClient: boolean;
  submissionCounts: Record<string, number>;
};

export function CycleView({
  coffeeBar,
  sessionState,
  identity,
  hasIdentity,
  onBackToBar,
  session,
  cycle,
  availableIngredients,
  isClient,
  submissionCounts,
}: CycleViewProps) {
  return (
    <div className={styles.cycleView}>
      <div className={styles.cycleToolbar}>
        <button
          type="button"
          className={sharedStyles.linkButton}
          onClick={onBackToBar}
        >
          ← Back to bar management
        </button>
        <div className={styles.cycleToolbarStatus}>{session.statusMessage}</div>
      </div>

      <section className={sharedStyles.card}>
        <h2 className={sharedStyles.cardTitle}>Now playing</h2>
        {sessionState && session.cycleForPlayer ? (
          <div className={styles.playerArea}>
            <div className={styles.playerColumn}>
              <div className={styles.playerWrapper}>
                {isClient ? (
                  <YouTubeEmbed
                    videoId={session.cycleForPlayer.videoId}
                    title={
                      session.cycleForPlayer.videoTitle ?? "Coffee Talk Video"
                    }
                    className={styles.player}
                  />
                ) : (
                  <div className={styles.playerPlaceholder}>
                    Loading the player…
                  </div>
                )}
              </div>
              {session.cycleForPlayer.videoTitle && (
                <div className={styles.videoTitle}>
                  {session.cycleForPlayer.videoTitle}
                </div>
              )}
            </div>

            <aside className={styles.voteSidebar}>
              <div className={styles.sidebarSection}>
                <div className={styles.sidebarHeading}>Cycle controls</div>
                <p className={sharedStyles.sessionStatus}>
                  {session.statusMessage}
                </p>
                <div className={styles.cycleButtons}>
                  <button
                    type="button"
                    className={`${sharedStyles.primaryButton} ${sharedStyles.cycleAction}`}
                    onClick={cycle.handleReveal}
                    disabled={
                      !hasIdentity ||
                      !session.activeCycle ||
                      cycle.revealLoading ||
                      !session.hasActiveSession
                    }
                  >
                    {cycle.revealLoading
                      ? "Revealing…"
                      : "Close voting & reveal"}
                  </button>
                  <button
                    type="button"
                    className={`${sharedStyles.secondaryButton} ${sharedStyles.cycleAction}`}
                    onClick={cycle.handleStartNextCycle}
                    disabled={
                      !hasIdentity ||
                      Boolean(session.activeCycle) ||
                      cycle.nextCycleLoading ||
                      availableIngredients === 0 ||
                      !session.hasActiveSession
                    }
                  >
                    {cycle.nextCycleLoading ? "Queuing…" : "Start next video"}
                  </button>
                </div>
              </div>

              <div className={styles.sidebarSection}>
                <div className={styles.sidebarHeading}>Who’s the curator?</div>
                <div className={styles.voteSummary}>
                  {session.activeCycle
                    ? `Votes: ${cycle.votesCast}/${cycle.totalVotesNeeded}`
                    : session.hasActiveSession
                    ? "Voting is closed for this video."
                    : "Session is not active."}
                </div>
                {cycle.displayReveal ? (
                  <>
                    <div className={styles.revealSummary}>
                      <div>
                        <strong>Curator:</strong>{" "}
                        {cycle.displayReveal.correctSubmitterIds
                          .map(
                            (hipsterId) =>
                              cycle.hipsterNameById.get(hipsterId) ?? "Unknown"
                          )
                          .join(", ")}
                      </div>
                      <div>
                        <strong>Correct guessers:</strong>{" "}
                        {cycle.displayReveal.correctGuessers.length > 0
                          ? cycle.displayReveal.correctGuessers
                              .map(
                                (hipsterId) =>
                                  cycle.hipsterNameById.get(hipsterId) ??
                                  "Unknown"
                              )
                              .join(", ")
                          : "No one guessed it this time."}
                      </div>
                    </div>
                    <ul className={styles.tallyList}>
                      {Object.entries(cycle.displayReveal.tally)
                        .sort(([, a], [, b]) => b - a)
                        .map(([hipsterId, votes]) => (
                          <li key={hipsterId} className={styles.tallyRow}>
                            <span>
                              {cycle.hipsterNameById.get(hipsterId) ??
                                "Unknown"}
                            </span>
                            <span className={styles.tallyCount}>{votes}</span>
                          </li>
                        ))}
                    </ul>
                  </>
                ) : hasIdentity ? (
                  session.activeCycle ? (
                    <ul className={styles.voteList}>
                      {coffeeBar.hipsters.map((hipster) => (
                        <li key={hipster.id}>
                          <button
                            type="button"
                            className={styles.voteButton}
                            disabled={
                              hipster.id === identity?.hipsterId ||
                              cycle.alreadyVoted ||
                              cycle.revealLoading
                            }
                            onClick={() => cycle.handleVote(hipster.id)}
                          >
                            {hipster.username}
                          </button>
                        </li>
                      ))}
                    </ul>
                  ) : session.hasActiveSession ? (
                    <p className={sharedStyles.sessionHint}>
                      Voting will resume on the next video.
                    </p>
                  ) : (
                    <p className={sharedStyles.sessionHint}>
                      Start a new session to vote again.
                    </p>
                  )
                ) : (
                  <p className={sharedStyles.sessionHint}>
                    Join the bar to cast your vote.
                  </p>
                )}
              </div>

              <div className={styles.sidebarSection}>
                <div className={styles.sidebarHeading}>Hipsters in the bar</div>
                <ul className={styles.sidebarHipsterList}>
                  {coffeeBar.hipsters.length === 0 && (
                    <li>No hipsters yet. Be the first to join!</li>
                  )}
                  {coffeeBar.hipsters.map((hipster) => (
                    <li
                      key={hipster.id}
                      className={
                        identity?.hipsterId === hipster.id
                          ? sharedStyles.me
                          : undefined
                      }
                    >
                      <span className={sharedStyles.hipsterName}>
                        {hipster.username}
                      </span>
                      <span className={sharedStyles.hipsterCount}>
                        {submissionCounts[hipster.id] ?? 0} urls
                      </span>
                    </li>
                  ))}
                </ul>
              </div>
            </aside>
          </div>
        ) : (
          <p className={sharedStyles.sessionHint}>
            Start a session to spin up the first video.
          </p>
        )}
      </section>

      {cycle.voteError && (
        <div className={sharedStyles.inlineError}>{cycle.voteError}</div>
      )}
    </div>
  );
}
