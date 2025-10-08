import { type HipsterIdentity } from "@/lib/identity";
import sharedStyles from "@/app/coffee-bars/[code]/CoffeeBarShared.module.css";
import styles from "@/app/coffee-bars/[code]/_components/LeaderboardView.module.css";
import {
  OVERALL_LEADERBOARD,
  type UseCoffeeBarClientResult,
} from "@/app/coffee-bars/[code]/useCoffeeBarClient";

type LeaderboardViewProps = {
  identity: HipsterIdentity | null;
  leaderboard: UseCoffeeBarClientResult["leaderboardState"];
  isActive: boolean;
};

export function LeaderboardView({
  identity,
  leaderboard,
  isActive,
}: LeaderboardViewProps) {
  const renderTrendIndicator = (
    entry: (typeof leaderboard.displayedEntries)[number]
  ) => {
    if (!leaderboard.showTrendColumn) {
      return "—";
    }

    if (entry.previousRank == null) {
      return "—";
    }

    if (entry.trend === "up") {
      const delta = entry.previousRank - entry.rank;
      const title =
        delta === 1
          ? "Up 1 place since last cycle"
          : `Up ${delta} places since last cycle`;
      return (
        <span
          className={`${styles.leaderboardTrendIndicator} ${styles.leaderboardTrendUp}`}
          title={title}
        >
          ↑{delta}
        </span>
      );
    }

    if (entry.trend === "down") {
      const delta = entry.rank - entry.previousRank;
      const title =
        delta === 1
          ? "Down 1 place since last cycle"
          : `Down ${delta} places since last cycle`;
      return (
        <span
          className={`${styles.leaderboardTrendIndicator} ${styles.leaderboardTrendDown}`}
          title={title}
        >
          ↓{delta}
        </span>
      );
    }

    return (
      <span
        className={styles.leaderboardTrendIndicator}
        title="No change since last cycle"
      >
        →
      </span>
    );
  };

  return (
    <div className={styles.leaderboardView}>
      <section className={sharedStyles.card}>
        <div className={styles.leaderboardHeader}>
          <div>
            <h2 className={sharedStyles.cardTitle}>Leaderboard</h2>
            <p className={styles.leaderboardHint}>
              Track the sharpest guessers across this bar or drill into a single
              session.
            </p>
          </div>
          <div className={styles.leaderboardControls}>
            <label className={styles.leaderboardLabel}>
              View
              <select
                className={styles.leaderboardSelect}
                value={leaderboard.selectedSessionId}
                onChange={leaderboard.handleSessionChange}
              >
                <option value={OVERALL_LEADERBOARD}>
                  Overall (all sessions)
                </option>
                {leaderboard.sortedSessions.map((session) => {
                  const startedAt = new Date(
                    session.startedAt
                  ).toLocaleString();
                  const status = session.endedAt ? "ended" : "active";
                  return (
                    <option key={session.sessionId} value={session.sessionId}>
                      {`Session • ${startedAt} (${status})`}
                    </option>
                  );
                })}
              </select>
            </label>
            <button
              type="button"
              className={sharedStyles.secondaryButton}
              onClick={leaderboard.refresh}
              disabled={leaderboard.loading}
            >
              {leaderboard.loading ? "Refreshing…" : "Refresh"}
            </button>
          </div>
        </div>
        {leaderboard.error ? (
          <div className={sharedStyles.inlineError}>{leaderboard.error}</div>
        ) : null}
        {isActive && leaderboard.loading && !leaderboard.leaderboard ? (
          <div className={styles.leaderboardStatus}>Loading standings…</div>
        ) : null}
        {leaderboard.myTrendMessage &&
        leaderboard.selectedSessionId === OVERALL_LEADERBOARD ? (
          <p className={styles.leaderboardTrend}>
            {leaderboard.myTrendMessage}
          </p>
        ) : null}
        <div className={styles.leaderboardTableWrapper}>
          <table className={styles.leaderboardTable}>
            <thead>
              <tr>
                <th scope="col">Rank</th>
                <th scope="col">Hipster</th>
                <th scope="col" className={styles.leaderboardTrendColumn}>
                  Trend
                </th>
                <th scope="col" className={styles.leaderboardScoreColumn}>
                  Correct guesses
                </th>
              </tr>
            </thead>
            <tbody>
              {leaderboard.displayedEntries.map((entry) => {
                const isMe = identity?.hipsterId === entry.hipsterId;
                const rowClassName = isMe
                  ? `${styles.leaderboardRow} ${styles.leaderboardMeRow}`
                  : styles.leaderboardRow;

                return (
                  <tr key={entry.hipsterId} className={rowClassName}>
                    <td className={styles.leaderboardRankCell}>{entry.rank}</td>
                    <td className={styles.leaderboardNameCell}>
                      {entry.username}
                    </td>
                    <td className={styles.leaderboardTrendCell}>
                      {renderTrendIndicator(entry)}
                    </td>
                    <td className={styles.leaderboardScoreCell}>
                      {entry.score}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        {leaderboard.displayedEntries.length === 0 &&
        !leaderboard.loading &&
        !leaderboard.error ? (
          <div className={styles.leaderboardStatus}>
            No standings yet. Reveal a cycle to see points.
          </div>
        ) : null}
        {leaderboard.selectedSessionSummary &&
        leaderboard.selectedSessionId !== OVERALL_LEADERBOARD ? (
          <p className={styles.leaderboardSessionMeta}>
            {leaderboard.selectedSessionSummary}
          </p>
        ) : null}
      </section>
    </div>
  );
}
