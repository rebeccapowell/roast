"use client";

import type { HubConnection } from "@microsoft/signalr";
import { ChangeEvent, FormEvent, memo, useCallback, useEffect, useMemo, useState } from "react";
import styles from "./CoffeeBarClient.module.css";
import { getIdentity, removeIdentity, saveIdentity, type HipsterIdentity } from "../../lib/identity";

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/$/, "");

type SubmissionPolicy = "LockOnFirstBrew" | "AlwaysOpen";

type HipsterResource = {
  id: string;
  username: string;
  maxIngredientQuota: number;
};

type IngredientResource = {
  id: string;
  videoId: string;
  isConsumed: boolean;
  submitterIds: string[];
  title: string | null;
  thumbnailUrl: string | null;
};

type SubmissionResource = {
  id: string;
  ingredientId: string;
  hipsterId: string;
  submittedAt: string;
};

type CoffeeBarResource = {
  id: string;
  code: string;
  theme: string;
  defaultMaxIngredientsPerHipster: number;
  submissionPolicy: SubmissionPolicy;
  submissionsLocked: boolean;
  isClosed: boolean;
  activeSessionId: string | null;
  hipsters: HipsterResource[];
  ingredients: IngredientResource[];
  submissions: SubmissionResource[];
};

type SubmitIngredientResponse = {
  coffeeBar: CoffeeBarResource;
  ingredient: IngredientResource;
  submissionId: string;
};

type JoinCoffeeBarResponse = {
  coffeeBar: CoffeeBarResource;
  hipster: HipsterResource;
};

type VoteResource = {
  id: string;
  voterHipsterId: string;
  targetHipsterId: string;
  castAt: string;
  isCorrect: boolean | null;
};

type BrewCycleResource = {
  id: string;
  sessionId: string;
  ingredientId: string;
  videoId: string;
  videoTitle: string | null;
  thumbnailUrl: string | null;
  startedAt: string;
  revealedAt: string | null;
  isActive: boolean;
  votes: VoteResource[];
  submitterIds: string[];
};

type BrewSessionResource = {
  id: string;
  startedAt: string;
  endedAt: string | null;
  cycles: BrewCycleResource[];
};

type SessionStateResource = {
  coffeeBar: CoffeeBarResource;
  session: BrewSessionResource;
};

type RevealResultResource = {
  cycleId: string;
  tally: Record<string, number>;
  correctSubmitterIds: string[];
  correctGuessers: string[];
};

type RevealCycleResponse = {
  session: SessionStateResource;
  reveal: RevealResultResource;
};

type LeaderboardTrend = "stable" | "up" | "down";

type LeaderboardEntryResource = {
  hipsterId: string;
  username: string;
  score: number;
  rank: number;
  previousRank: number | null;
  trend: LeaderboardTrend;
};

type SessionLeaderboardResource = {
  sessionId: string;
  startedAt: string;
  endedAt: string | null;
  entries: LeaderboardEntryResource[];
};

type CoffeeBarLeaderboardResource = {
  overall: LeaderboardEntryResource[];
  sessions: SessionLeaderboardResource[];
};

type CoffeeBarClientProps = {
  code: string;
};

type YouTubeEmbedProps = {
  videoId: string;
  title: string;
  className?: string;
};

const YouTubeEmbed = memo(function YouTubeEmbed({ videoId, title, className }: YouTubeEmbedProps) {
  const src = useMemo(
    () => `https://www.youtube.com/embed/${videoId}?rel=0&modestbranding=1`,
    [videoId],
  );

  return (
    <iframe
      className={className}
      src={src}
      title={title}
      allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
      allowFullScreen
    />
  );
});

export function CoffeeBarClient({ code }: CoffeeBarClientProps) {
  const normalizedCode = code.toUpperCase();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [coffeeBar, setCoffeeBar] = useState<CoffeeBarResource | null>(null);
  const [sessionState, setSessionState] = useState<SessionStateResource | null>(null);
  const [identity, setIdentity] = useState<HipsterIdentity | null>(null);
  const [joinUsername, setJoinUsername] = useState("");
  const [joinLoading, setJoinLoading] = useState(false);
  const [joinError, setJoinError] = useState<string | null>(null);
  const [urlInput, setUrlInput] = useState("");
  const [submissionLoading, setSubmissionLoading] = useState(false);
  const [sessionLoading, setSessionLoading] = useState(false);
  const [endSessionLoading, setEndSessionLoading] = useState(false);
  const [voteError, setVoteError] = useState<string | null>(null);
  const [revealLoading, setRevealLoading] = useState(false);
  const [nextCycleLoading, setNextCycleLoading] = useState(false);
  const [activeView, setActiveView] = useState<"bar" | "cycle" | "leaderboard">("bar");
  const [revealResult, setRevealResult] = useState<RevealResultResource | null>(null);
  const [lastCycleId, setLastCycleId] = useState<string | null>(null);
  const [realtimeConnected, setRealtimeConnected] = useState(false);
  const [playerCycle, setPlayerCycle] = useState<BrewCycleResource | null>(null);
  const [isClient, setIsClient] = useState(false);
  const OVERALL_LEADERBOARD = "overall" as const;
  const [leaderboard, setLeaderboard] = useState<CoffeeBarLeaderboardResource | null>(null);
  const [leaderboardLoading, setLeaderboardLoading] = useState(false);
  const [leaderboardError, setLeaderboardError] = useState<string | null>(null);
  const [leaderboardStale, setLeaderboardStale] = useState(true);
  const [selectedLeaderboardSessionId, setSelectedLeaderboardSessionId] = useState<string>(
    OVERALL_LEADERBOARD,
  );

  const loadIdentity = useCallback(() => {
    const stored = getIdentity(normalizedCode);
    if (stored) {
      setIdentity(stored);
    }
  }, [normalizedCode]);

  const requestCoffeeBar = useCallback(async () => {
    const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}`);
    if (!response.ok) {
      if (response.status === 404) {
        throw new Error("We couldn't find this coffee bar. Check the code and try again.");
      }

      const payload = await response.json().catch(() => null);
      throw new Error((payload && (payload.detail ?? payload.title)) || "Unable to load this coffee bar.");
    }

    return (await response.json()) as CoffeeBarResource;
  }, [normalizedCode]);

  const fetchCoffeeBar = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await requestCoffeeBar();
      setCoffeeBar(data);
      return data;
    } catch (err) {
      console.error(err);
      setError(err instanceof Error ? err.message : "Unable to load this coffee bar.");
      return null;
    } finally {
      setLoading(false);
    }
  }, [requestCoffeeBar]);

  const refreshCoffeeBar = useCallback(async () => {
    try {
      const data = await requestCoffeeBar();
      setCoffeeBar(data);
      return data;
    } catch (err) {
      console.error(err);
      return null;
    }
  }, [requestCoffeeBar]);

  const fetchLeaderboard = useCallback(async () => {
    setLeaderboardLoading(true);
    setLeaderboardError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/leaderboard`);
      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(
          (payload && (payload.detail ?? payload.title)) || "Unable to load leaderboard standings.",
        );
      }

      const payload = (await response.json()) as CoffeeBarLeaderboardResource;
      setLeaderboard(payload);
    } catch (err) {
      console.error(err);
      setLeaderboardError(
        err instanceof Error ? err.message : "Unable to load leaderboard standings.",
      );
    } finally {
      setLeaderboardLoading(false);
      setLeaderboardStale(false);
    }
  }, [normalizedCode]);

  useEffect(() => {
    loadIdentity();
  }, [loadIdentity]);

  useEffect(() => {
    fetchCoffeeBar();
  }, [fetchCoffeeBar]);

  useEffect(() => {
    setLeaderboard(null);
    setLeaderboardError(null);
    setLeaderboardStale(true);
    setSelectedLeaderboardSessionId(OVERALL_LEADERBOARD);
  }, [normalizedCode]);

  useEffect(() => {
    setIsClient(true);
  }, []);

  useEffect(() => {
    if (coffeeBar || sessionState) {
      setLeaderboardStale(true);
    }
  }, [coffeeBar, sessionState]);

  useEffect(() => {
    if (activeView !== "leaderboard" || !leaderboardStale || leaderboardLoading) {
      return;
    }

    void fetchLeaderboard();
  }, [activeView, leaderboardStale, leaderboardLoading, fetchLeaderboard]);

  useEffect(() => {
    if (!leaderboard) {
      return;
    }

    if (
      selectedLeaderboardSessionId !== OVERALL_LEADERBOARD &&
      !leaderboard.sessions.some((session) => session.sessionId === selectedLeaderboardSessionId)
    ) {
      setSelectedLeaderboardSessionId(OVERALL_LEADERBOARD);
    }
  }, [leaderboard, selectedLeaderboardSessionId, OVERALL_LEADERBOARD]);

  useEffect(() => {
    if (!API_BASE_URL) {
      return;
    }

    let isActive = true;
    let connection: HubConnection | null = null;

    const handleCoffeeBarUpdated = (resource: CoffeeBarResource) => {
      if (!isActive) {
        return;
      }

      setCoffeeBar(resource);
      setSessionState((current) => (current ? { ...current, coffeeBar: resource } : current));
    };

    const handleSessionUpdated = (resource: SessionStateResource) => {
      if (!isActive) {
        return;
      }

      setCoffeeBar(resource.coffeeBar);
      setSessionState(resource);
    };

    const handleCycleRevealed = (response: RevealCycleResponse) => {
      if (!isActive) {
        return;
      }

      setCoffeeBar(response.session.coffeeBar);
      setSessionState(response.session);
      setRevealResult(response.reveal);
      setLastCycleId(response.session.session.cycles.at(-1)?.id ?? null);
      setActiveView("cycle");
    };

    const handleReconnecting = () => {
      if (!isActive) {
        return;
      }

      setRealtimeConnected(false);
    };

    const handleClosed = () => {
      if (!isActive) {
        return;
      }

      setRealtimeConnected(false);
    };

    const handleReconnected = async () => {
      if (!isActive) {
        return;
      }

      setRealtimeConnected(true);
      try {
        await connection?.invoke("JoinCoffeeBar", normalizedCode);
      } catch (err) {
        console.error("Failed to rejoin coffee bar group", err);
      }
    };

    const setupConnection = async () => {
      try {
        const signalR = await import("@microsoft/signalr");
        if (!isActive) {
          return;
        }

        connection = new signalR.HubConnectionBuilder()
          .withUrl(`${API_BASE_URL}/hubs/coffee-bar`)
          .withAutomaticReconnect()
          .configureLogging(signalR.LogLevel.Warning)
          .build();

        connection.on("CoffeeBarUpdated", handleCoffeeBarUpdated);
        connection.on("SessionUpdated", handleSessionUpdated);
        connection.on("CycleRevealed", handleCycleRevealed);
        connection.onreconnecting(handleReconnecting);
        connection.onclose(handleClosed);
        connection.onreconnected(handleReconnected);

        try {
          await connection.start();
          if (!isActive) {
            return;
          }

          setRealtimeConnected(true);
          await connection.invoke("JoinCoffeeBar", normalizedCode);
        } catch (err) {
          if (!isActive) {
            return;
          }

          console.error("Failed to establish realtime connection", err);
          setRealtimeConnected(false);
        }
      } catch (err) {
        if (!isActive) {
          return;
        }

        console.error("Failed to load realtime client", err);
        setRealtimeConnected(false);
      }
    };

    void setupConnection();

    return () => {
      isActive = false;
      setRealtimeConnected(false);

      if (!connection) {
        return;
      }

      connection.off("CoffeeBarUpdated", handleCoffeeBarUpdated);
      connection.off("SessionUpdated", handleSessionUpdated);
      connection.off("CycleRevealed", handleCycleRevealed);
      connection.onreconnecting(() => {});
      connection.onclose(() => {});
      connection.onreconnected(() => {});
      void connection.invoke("LeaveCoffeeBar", normalizedCode).catch(() => {});
      void connection.stop().catch(() => {});
    };
  }, [normalizedCode]);

  useEffect(() => {
    if (!coffeeBar || !identity) {
      return;
    }

    const stillPresent = coffeeBar.hipsters.some((hipster) => hipster.id === identity.hipsterId);
    if (!stillPresent) {
      removeIdentity(coffeeBar.code);
      setIdentity(null);
    }
  }, [coffeeBar, identity]);

  const ingredientById = useMemo(() => {
    if (!coffeeBar) {
      return new Map<string, IngredientResource>();
    }

    return new Map(coffeeBar.ingredients.map((ingredient) => [ingredient.id, ingredient] as const));
  }, [coffeeBar]);

  const submissionCounts = useMemo(() => {
    const counts: Record<string, number> = {};
    if (!coffeeBar) {
      return counts;
    }

    for (const submission of coffeeBar.submissions) {
      counts[submission.hipsterId] = (counts[submission.hipsterId] ?? 0) + 1;
    }

    return counts;
  }, [coffeeBar]);

  const availableIngredients = useMemo(() => {
    if (!coffeeBar) {
      return 0;
    }

    return coffeeBar.ingredients.filter((ingredient) => !ingredient.isConsumed).length;
  }, [coffeeBar]);

  const sessionEndedAt = sessionState?.session.endedAt ?? null;
  const hasActiveSession = Boolean(sessionState && !sessionEndedAt);

  const sessionCycles = useMemo(() => sessionState?.session.cycles ?? [], [sessionState]);

  const activeCycle = useMemo(() => {
    if (!hasActiveSession) {
      return null;
    }

    return sessionCycles.find((cycle) => cycle.revealedAt === null) ?? null;
  }, [hasActiveSession, sessionCycles]);

  const latestCycle = useMemo(
    () => (sessionCycles.length ? sessionCycles[sessionCycles.length - 1] : null),
    [sessionCycles],
  );

  useEffect(() => {
    if (latestCycle) {
      setPlayerCycle((current) => (current && current.id === latestCycle.id ? current : latestCycle));
    } else if (!sessionState) {
      setPlayerCycle(null);
    }
  }, [latestCycle, sessionState]);

  const cycleForPlayer = playerCycle ?? activeCycle ?? latestCycle;

  const hipsterNameById = useMemo(() => {
    if (!coffeeBar) {
      return new Map<string, string>();
    }

    return new Map(coffeeBar.hipsters.map((hipster) => [hipster.id, hipster.username] as const));
  }, [coffeeBar]);

  const sortedLeaderboardSessions = useMemo(() => {
    if (!leaderboard) {
      return [] as SessionLeaderboardResource[];
    }

    return [...leaderboard.sessions].sort(
      (a, b) => new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime(),
    );
  }, [leaderboard]);

  const displayedLeaderboardEntries = useMemo(() => {
    if (!leaderboard) {
      return [] as LeaderboardEntryResource[];
    }

    if (selectedLeaderboardSessionId === OVERALL_LEADERBOARD) {
      return leaderboard.overall;
    }

    return (
      leaderboard.sessions.find((session) => session.sessionId === selectedLeaderboardSessionId)?.entries ?? []
    );
  }, [leaderboard, selectedLeaderboardSessionId, OVERALL_LEADERBOARD]);

  const selectedLeaderboardSession = useMemo(() => {
    if (!leaderboard || selectedLeaderboardSessionId === OVERALL_LEADERBOARD) {
      return null;
    }

    return leaderboard.sessions.find((session) => session.sessionId === selectedLeaderboardSessionId) ?? null;
  }, [leaderboard, selectedLeaderboardSessionId, OVERALL_LEADERBOARD]);

  const showTrendColumn = selectedLeaderboardSessionId === OVERALL_LEADERBOARD;

  const myLeaderboardEntry = useMemo(() => {
    if (!identity || !leaderboard) {
      return null;
    }

    return leaderboard.overall.find((entry) => entry.hipsterId === identity.hipsterId) ?? null;
  }, [identity, leaderboard]);

  const myTrendMessage = useMemo(() => {
    if (!myLeaderboardEntry) {
      return null;
    }

    switch (myLeaderboardEntry.trend) {
      case "up": {
        const change = myLeaderboardEntry.previousRank
          ? myLeaderboardEntry.previousRank - myLeaderboardEntry.rank
          : 0;
        return change > 0
          ? `You're climbing the leaderboard (up ${change} place${change === 1 ? "" : "s"}). Keep it brewing!`
          : "You're climbing the leaderboard. Keep it brewing!";
      }
      case "down": {
        const change = myLeaderboardEntry.previousRank
          ? myLeaderboardEntry.rank - myLeaderboardEntry.previousRank
          : 0;
        return change > 0
          ? `You're slipping ${change} place${change === 1 ? "" : "s"} on the leaderboard. Time for a comeback.`
          : "You're slipping on the leaderboard. Time for a comeback.";
      }
      default:
        return "You're holding steady on the leaderboard.";
    }
  }, [myLeaderboardEntry]);

  const selectedLeaderboardSummary = useMemo(() => {
    if (!selectedLeaderboardSession) {
      return null;
    }

    const started = new Date(selectedLeaderboardSession.startedAt).toLocaleString();
    if (selectedLeaderboardSession.endedAt) {
      const ended = new Date(selectedLeaderboardSession.endedAt).toLocaleString();
      return `Session ran from ${started} to ${ended}.`;
    }

    return `Session started ${started} and is still active.`;
  }, [selectedLeaderboardSession]);

  const renderTrendIndicator = useCallback(
    (entry: LeaderboardEntryResource) => {
      if (!showTrendColumn) {
        return "—";
      }

      if (entry.previousRank == null) {
        return "—";
      }

      if (entry.trend === "up") {
        const delta = entry.previousRank - entry.rank;
        const title = delta === 1 ? "Up 1 place since last cycle" : `Up ${delta} places since last cycle`;
        return (
          <span className={`${styles.leaderboardTrendIndicator} ${styles.leaderboardTrendUp}`} title={title}>
            ↑{delta}
          </span>
        );
      }

      if (entry.trend === "down") {
        const delta = entry.rank - entry.previousRank;
        const title = delta === 1 ? "Down 1 place since last cycle" : `Down ${delta} places since last cycle`;
        return (
          <span className={`${styles.leaderboardTrendIndicator} ${styles.leaderboardTrendDown}`} title={title}>
            ↓{delta}
          </span>
        );
      }

      return (
        <span className={styles.leaderboardTrendIndicator} title="No change since last cycle">
          →
        </span>
      );
    },
    [showTrendColumn],
  );

  const handleLeaderboardSessionChange = useCallback((event: ChangeEvent<HTMLSelectElement>) => {
    setSelectedLeaderboardSessionId(event.target.value);
  }, []);

  const handleRefreshLeaderboard = useCallback(() => {
    if (leaderboardLoading) {
      return;
    }

    setLeaderboardStale(true);
  }, [leaderboardLoading]);

  const derivedReveal = useMemo(() => {
    if (!latestCycle || latestCycle.isActive) {
      return null;
    }

    const tally: Record<string, number> = {};
    for (const vote of latestCycle.votes) {
      tally[vote.targetHipsterId] = (tally[vote.targetHipsterId] ?? 0) + 1;
    }

    const correctGuessers = latestCycle.votes
      .filter((vote) => vote.isCorrect === true)
      .map((vote) => vote.voterHipsterId);

    return {
      cycleId: latestCycle.id,
      tally,
      correctSubmitterIds: latestCycle.submitterIds,
      correctGuessers,
    } satisfies RevealResultResource;
  }, [latestCycle]);

  const displayReveal = revealResult ?? derivedReveal;

  const totalVotesNeeded = hasActiveSession ? coffeeBar?.hipsters.length ?? 0 : 0;
  const votesCast = activeCycle?.votes.length ?? 0;
  const hasIdentity = Boolean(identity);
  const alreadyVoted = Boolean(
    identity && activeCycle && activeCycle.votes.some((vote) => vote.voterHipsterId === identity.hipsterId),
  );

  const cycleStatusMessage = useMemo(() => {
    if (hasActiveSession) {
      if (activeCycle) {
        return "Voting is open. Close it when your crew is ready.";
      }

      if (latestCycle) {
        return "Voting is closed. Reveal results or move to the next video.";
      }

      return "No cycle is active yet.";
    }

    if (sessionState) {
      return "Session has ended. Start a new session to brew again.";
    }

    return "No cycle is active yet.";
  }, [activeCycle, hasActiveSession, latestCycle, sessionState]);

  const shareLink = useMemo(() => {
    if (!coffeeBar || typeof window === "undefined") {
      return "";
    }

    return `${window.location.origin}/coffee-bars/${coffeeBar.code}`;
  }, [coffeeBar]);

  const handleJoin = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();

      const trimmed = joinUsername.trim();
      if (!trimmed) {
        setJoinError("Please choose a username to join the bar.");
        return;
      }

      setJoinLoading(true);
      setJoinError(null);

      try {
        const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/hipsters`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ username: trimmed }),
        });

        let payload: any = await response.json().catch(() => null);
        let effective = response;
        // If joining fails (e.g., username already exists), try rejoining to fetch existing hipsterId
        if (!response.ok && response.status === 400) {
          const rejoin = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/hipsters:rejoin`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: trimmed }),
          });
          if (rejoin.ok) {
            effective = rejoin;
            payload = await rejoin.json().catch(() => null);
          }
        }

        if (!effective.ok) {
          setJoinError((payload && (payload.detail ?? payload.title)) || "We couldn't join that coffee bar.");
          return;
        }

        const joined = payload as JoinCoffeeBarResponse;
        setCoffeeBar(joined.coffeeBar);
        saveIdentity({
          coffeeBarId: joined.coffeeBar.id,
          coffeeBarCode: joined.coffeeBar.code,
          hipsterId: joined.hipster.id,
          username: joined.hipster.username,
        });
        setIdentity({
          coffeeBarId: joined.coffeeBar.id,
          coffeeBarCode: joined.coffeeBar.code,
          hipsterId: joined.hipster.id,
          username: joined.hipster.username,
        });
        setJoinUsername("");
      } catch (err) {
        console.error(err);
        setJoinError("Something went wrong while joining. Please try again.");
      } finally {
        setJoinLoading(false);
      }
    },
    [joinUsername, normalizedCode],
  );

  const handleLeave = useCallback(() => {
    removeIdentity(normalizedCode);
    setIdentity(null);
    setJoinUsername("");
    setJoinError(null);
  }, [normalizedCode]);

  const handleSubmitIngredient = useCallback(
    async (event: FormEvent<HTMLFormElement>) => {
      event.preventDefault();
      if (!identity) {
        return;
      }

      const trimmed = urlInput.trim();
      if (!trimmed) {
        return;
      }

      setSubmissionLoading(true);
      setVoteError(null);

      try {
        const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/ingredients`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ hipsterId: identity.hipsterId, url: trimmed }),
        });

        const payload = await response.json();
        if (!response.ok) {
          setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't submit that URL.");
          return;
        }

        const submission = payload as SubmitIngredientResponse;
        setCoffeeBar(submission.coffeeBar);
        setSessionState((current) => (current ? { ...current, coffeeBar: submission.coffeeBar } : current));
        setUrlInput("");
      } catch (err) {
        console.error(err);
        setVoteError("Something went wrong while submitting. Please try again.");
      } finally {
        setSubmissionLoading(false);
      }
    },
    [identity, normalizedCode, urlInput],
  );

  const handleRemoveSubmission = useCallback(
    async (submissionId: string) => {
      if (!identity) {
        return;
      }

      try {
        const response = await fetch(
          `${API_BASE_URL}/coffee-bars/${normalizedCode}/submissions/${submissionId}?hipsterId=${identity.hipsterId}`,
          {
            method: "DELETE",
          },
        );

        if (response.status === 404) {
          setVoteError("We couldn't find that submission anymore.");
          await refreshCoffeeBar();
          return;
        }

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
          setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't remove that submission.");
          return;
        }

        const updated = payload as CoffeeBarResource;
        setCoffeeBar(updated);
        setSessionState((current) => (current ? { ...current, coffeeBar: updated } : current));
      } catch (err) {
        console.error(err);
        setVoteError("Something went wrong while removing the URL.");
      }
    },
    [identity, normalizedCode, refreshCoffeeBar],
  );

  const refreshSession = useCallback(
    async (sessionId: string) => {
      try {
        const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${sessionId}`);
        if (!response.ok) {
          return;
        }

        const payload = (await response.json()) as SessionStateResource;
        setCoffeeBar(payload.coffeeBar);
        setSessionState(payload);
      } catch (err) {
        console.error(err);
      }
    },
    [normalizedCode],
  );

  useEffect(() => {
    if (!coffeeBar || !coffeeBar.activeSessionId) {
      return;
    }

    if (sessionState && sessionState.session.id === coffeeBar.activeSessionId) {
      return;
    }

    void refreshSession(coffeeBar.activeSessionId);
  }, [coffeeBar, refreshSession, sessionState]);

  useEffect(() => {
    if (realtimeConnected) {
      return;
    }

    if (!coffeeBar && (!sessionState || !hasActiveSession)) {
      return;
    }

    const interval = window.setInterval(() => {
      if (coffeeBar) {
        void refreshCoffeeBar();
      }

      if (sessionState && hasActiveSession) {
        void refreshSession(sessionState.session.id);
      }
    }, 5000);

    return () => window.clearInterval(interval);
  }, [coffeeBar, hasActiveSession, realtimeConnected, refreshCoffeeBar, refreshSession, sessionState]);

  useEffect(() => {
    if (!sessionState) {
      setActiveView("bar");
      setRevealResult(null);
      setLastCycleId(null);
      return;
    }

    if (!hasActiveSession) {
      setActiveView("bar");
      return;
    }

    if (!latestCycle) {
      return;
    }

    if (latestCycle.id !== lastCycleId) {
      setLastCycleId(latestCycle.id);
      setRevealResult(null);
      setActiveView("cycle");
    }
  }, [sessionState, latestCycle, lastCycleId, hasActiveSession]);

  const handleStartSession = useCallback(async () => {
    if (!coffeeBar || hasActiveSession) {
      return;
    }

    setSessionLoading(true);
    setVoteError(null);

    try {
      const response = await fetch(`${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions`, {
        method: "POST",
      });

      const payload = await response.json();
      if (!response.ok) {
        setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't start the session.");
        return;
      }

      const state = payload as SessionStateResource;
      setCoffeeBar(state.coffeeBar);
      setSessionState(state);
      setRevealResult(null);
      setLastCycleId(null);
      setPlayerCycle(null);
      setActiveView("cycle");
    } catch (err) {
      console.error(err);
      setVoteError("Something went wrong while starting the session.");
    } finally {
      setSessionLoading(false);
    }
  }, [coffeeBar, hasActiveSession, normalizedCode]);

  const handleEndSession = useCallback(async () => {
    if (!sessionState || !hasActiveSession) {
      return;
    }

    setEndSessionLoading(true);
    setVoteError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${sessionState.session.id}/end`,
        { method: "POST" },
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't stop the session.");
        return;
      }

      const state = payload as SessionStateResource;
      setCoffeeBar(state.coffeeBar);
      setSessionState(state);
      setRevealResult(null);
      setActiveView("bar");
    } catch (err) {
      console.error(err);
      setVoteError("Something went wrong while stopping the session.");
    } finally {
      setEndSessionLoading(false);
    }
  }, [hasActiveSession, normalizedCode, sessionState]);

  const handleCastVote = useCallback(
    async (targetHipsterId: string) => {
      if (!identity || !activeCycle) {
        return;
      }

      setVoteError(null);

      try {
        const response = await fetch(
          `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${activeCycle.sessionId}/cycles/${activeCycle.id}/votes`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ voterHipsterId: identity.hipsterId, targetHipsterId }),
          },
        );

        const payload = await response.json();
        if (!response.ok) {
          setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't cast that vote.");
          return;
        }

        const state = payload as SessionStateResource;
        setCoffeeBar(state.coffeeBar);
        setSessionState(state);
      } catch (err) {
        console.error(err);
        setVoteError("Something went wrong while casting your vote.");
      }
    },
    [identity, activeCycle, normalizedCode],
  );

  const handleRevealCycle = useCallback(async () => {
    if (!identity || !activeCycle) {
      return;
    }

    setRevealLoading(true);
    setVoteError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${activeCycle.sessionId}/cycles/${activeCycle.id}/reveal`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ hipsterId: identity.hipsterId }),
        },
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError((payload && (payload.detail ?? payload.title)) || "We couldn't reveal this cycle.");
        return;
      }

      const result = payload as RevealCycleResponse;
      setCoffeeBar(result.session.coffeeBar);
      setSessionState(result.session);
      setRevealResult(result.reveal);
      setActiveView("cycle");
    } catch (err) {
      console.error(err);
      setVoteError("Something went wrong while revealing the cycle.");
    } finally {
      setRevealLoading(false);
    }
  }, [identity, activeCycle, normalizedCode]);

  const handleStartNextCycle = useCallback(async () => {
    if (!identity || !sessionState || !hasActiveSession) {
      return;
    }

    setNextCycleLoading(true);
    setVoteError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${sessionState.session.id}/cycles`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ hipsterId: identity.hipsterId }),
        },
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError(
          (payload && (payload.detail ?? payload.title)) || "We couldn't start the next cycle.",
        );
        return;
      }

      const state = payload as SessionStateResource;
      setCoffeeBar(state.coffeeBar);
      setSessionState(state);
      setRevealResult(null);
      setActiveView("cycle");
    } catch (err) {
      console.error(err);
      setVoteError("Something went wrong while starting the next cycle.");
    } finally {
      setNextCycleLoading(false);
    }
  }, [hasActiveSession, identity, sessionState, normalizedCode]);

  const mySubmissions = useMemo(() => {
    if (!identity || !coffeeBar) {
      return [] as { submission: SubmissionResource; ingredient?: IngredientResource }[];
    }

    return coffeeBar.submissions
      .filter((submission) => submission.hipsterId === identity.hipsterId)
      .map((submission) => ({ submission, ingredient: ingredientById.get(submission.ingredientId) }));
  }, [coffeeBar, identity, ingredientById]);

  return (
    <div className={styles.page}>
      {loading && !coffeeBar ? (
        <div className={styles.loading}>Loading your coffee bar…</div>
      ) : error ? (
        <div className={styles.error}>{error}</div>
      ) : coffeeBar ? (
        <>
          <header className={styles.header}>
            <div className={styles.headerInfo}>
              <div className={styles.headerTop}>Coffee Bar · {coffeeBar.code}</div>
              <h1 className={styles.title}>{coffeeBar.theme}</h1>
            </div>
            <div className={styles.headerActions}>
              <p className={styles.shareHint}>
                Share this link with your crew: {" "}
                {shareLink ? (
                  <a className={styles.shareAnchor} href={shareLink}>
                    {shareLink}
                  </a>
                ) : (
                  "Copy the URL"
                )}
              </p>
              <p className={styles.policy}>
                Policy: <strong>{coffeeBar.submissionPolicy === "LockOnFirstBrew" ? "Lock on first brew" : "Always open"}</strong>
                {" · "}Max ingredients per hipster: <strong>{coffeeBar.defaultMaxIngredientsPerHipster}</strong>
              </p>
              <div className={styles.viewSwitcher}>
                <button
                  type="button"
                  className={`${styles.viewButton} ${activeView === "cycle" ? styles.viewButtonActive : ""}`}
                  onClick={() => setActiveView("cycle")}
                >
                  Cycle view
                </button>
                <button
                  type="button"
                  className={`${styles.viewButton} ${activeView === "bar" ? styles.viewButtonActive : ""}`}
                  onClick={() => setActiveView("bar")}
                >
                  Bar management
                </button>
                <button
                  type="button"
                  className={`${styles.viewButton} ${activeView === "leaderboard" ? styles.viewButtonActive : ""}`}
                  onClick={() => setActiveView("leaderboard")}
                >
                  Leaderboard
                </button>
              </div>
            </div>
          </header>

          {activeView === "bar" ? (
            <div className={styles.layout}>
              <aside className={styles.sidebar}>
                <section className={styles.card}>
                  <h2 className={styles.cardTitle}>Hipsters in the bar</h2>
                  <ul className={styles.hipsterList}>
                    {coffeeBar.hipsters.length === 0 && <li>No hipsters yet. Be the first to join!</li>}
                    {coffeeBar.hipsters.map((hipster) => (
                      <li key={hipster.id} className={identity?.hipsterId === hipster.id ? styles.me : undefined}>
                        <span className={styles.hipsterName}>{hipster.username}</span>
                        <span className={styles.hipsterCount}>{submissionCounts[hipster.id] ?? 0} urls</span>
                      </li>
                    ))}
                  </ul>
                </section>

                <section className={styles.card}>
                  <h2 className={styles.cardTitle}>Session</h2>
                  <p className={styles.sessionStatus}>
                    {hasActiveSession
                      ? `Session started ${new Date(sessionState!.session.startedAt).toLocaleTimeString()}`
                      : sessionState
                        ? `Session ended ${
                            sessionEndedAt
                              ? new Date(sessionEndedAt).toLocaleTimeString()
                              : "recently"
                          }`
                        : "No active session yet."}
                  </p>
                  {hasActiveSession ? (
                    <button
                      className={styles.secondaryButton}
                      type="button"
                      onClick={handleEndSession}
                      disabled={endSessionLoading || !hasIdentity || Boolean(activeCycle)}
                    >
                      {endSessionLoading ? "Stopping…" : "Stop session"}
                    </button>
                  ) : (
                    <button
                      className={styles.primaryButton}
                      type="button"
                      onClick={handleStartSession}
                      disabled={sessionLoading || availableIngredients === 0 || !hasIdentity}
                    >
                      {sessionLoading
                        ? "Starting…"
                        : sessionState
                          ? "Start new session"
                          : "Start session"}
                    </button>
                  )}
                  <p className={styles.sessionHint}>
                    {availableIngredients === 0
                      ? "Add more ingredients before brewing."
                      : `${availableIngredients} ingredient${availableIngredients === 1 ? "" : "s"} ready to brew.`}
                  </p>
                  {hasActiveSession && activeCycle && (
                    <p className={styles.sessionHint}>
                      Reveal the current video before stopping the session.
                    </p>
                  )}
                  {!hasIdentity && <p className={styles.sessionHint}>Join the bar to control the brew.</p>}
                </section>

                {!hasIdentity && (
                  <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Join this bar</h2>
                    <form className={styles.joinForm} onSubmit={handleJoin}>
                      <input
                        className={styles.input}
                        value={joinUsername}
                        onChange={(event) => setJoinUsername(event.target.value)}
                        placeholder="Your username"
                        required
                        minLength={3}
                        maxLength={20}
                        disabled={joinLoading}
                      />
                      <button className={styles.primaryButton} type="submit" disabled={joinLoading}>
                        {joinLoading ? "Joining…" : "Join"}
                      </button>
                    </form>
                    {joinError && <div className={styles.inlineError}>{joinError}</div>}
                  </section>
                )}

                {hasIdentity && identity && (
                  <section className={styles.card}>
                    <h2 className={styles.cardTitle}>You’re in</h2>
                    <p className={styles.identityLine}>
                      Signed in as <strong>{identity.username}</strong>
                    </p>
                    <button className={styles.secondaryButton} type="button" onClick={handleLeave}>
                      Leave this bar
                    </button>
                  </section>
                )}
              </aside>

              <main className={styles.main}>
                {hasIdentity ? (
                  <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Submit a new ingredient</h2>
                    <form className={styles.submitForm} onSubmit={handleSubmitIngredient}>
                      <input
                        className={styles.input}
                        value={urlInput}
                        onChange={(event) => setUrlInput(event.target.value)}
                        placeholder="Paste a YouTube URL"
                        required
                        disabled={submissionLoading || coffeeBar.submissionsLocked}
                      />
                      <button
                        className={styles.primaryButton}
                        type="submit"
                        disabled={submissionLoading || coffeeBar.submissionsLocked}
                      >
                        {submissionLoading ? "Submitting…" : "Submit"}
                      </button>
                    </form>
                    {coffeeBar.submissionsLocked && (
                      <p className={styles.sessionHint}>Submissions are locked while brewing.</p>
                    )}
                  </section>
                ) : (
                  <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Ready to brew?</h2>
                    <p className={styles.sessionHint}>
                      Join the bar from the left column to start submitting your favourite tracks.
                    </p>
                  </section>
                )}

                {hasIdentity && (
                  <section className={styles.card}>
                    <h2 className={styles.cardTitle}>Your submissions</h2>
                    {mySubmissions.length === 0 ? (
                      <p className={styles.sessionHint}>You haven’t queued any videos yet.</p>
                    ) : (
                      <ul className={styles.submissionList}>
                        {mySubmissions.map(({ submission, ingredient }) => (
                          <li key={submission.id}>
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
                                      className={styles.shareAnchor}
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
                                className={styles.secondaryButton}
                                onClick={() => handleRemoveSubmission(submission.id)}
                              >
                                Remove
                              </button>
                            </div>
                          </li>
                        ))}
                      </ul>
                    )}
                  </section>
                )}

                <section className={styles.card}>
                  <h2 className={styles.cardTitle}>Live cycle</h2>
                  {sessionState ? (
                    <>
                      <p className={styles.sessionHint}>
                        {hasActiveSession
                          ? activeCycle
                            ? "A video is brewing right now. Switch to the cycle view to manage voting."
                            : latestCycle
                              ? "Voting is closed for this video. Head to the cycle view to reveal results or start the next brew."
                              : "Session is ready to begin brewing."
                          : latestCycle
                            ? "Session has ended. Review the last brew or start a new session to keep going."
                            : "Session has ended. Start a new session to brew again."}
                      </p>
                      <button
                        type="button"
                        className={`${styles.primaryButton} ${styles.cycleAction}`}
                        onClick={() => setActiveView("cycle")}
                        disabled={!hasActiveSession && !latestCycle}
                      >
                        {hasActiveSession ? "Open cycle view" : "Review cycle view"}
                      </button>
                    </>
                  ) : (
                    <p className={styles.sessionHint}>Start a session to brew the first video.</p>
                  )}
                </section>

                {voteError && <div className={styles.inlineError}>{voteError}</div>}
              </main>
            </div>
          ) : activeView === "cycle" ? (
            <div className={styles.cycleView}>
              <div className={styles.cycleToolbar}>
                <button
                  type="button"
                  className={styles.linkButton}
                  onClick={() => setActiveView("bar")}
                >
                  ← Back to bar management
                </button>
                <div className={styles.cycleToolbarStatus}>{cycleStatusMessage}</div>
              </div>

              <section className={styles.card}>
                <h2 className={styles.cardTitle}>Now playing</h2>
                {sessionState && cycleForPlayer ? (
                  <div className={styles.playerArea}>
                    <div className={styles.playerColumn}>
                      <div className={styles.playerWrapper}>
                        {isClient ? (
                          <YouTubeEmbed
                            videoId={cycleForPlayer.videoId}
                            title={cycleForPlayer.videoTitle ?? "Coffee Talk Video"}
                            className={styles.player}
                          />
                        ) : (
                          <div className={styles.playerPlaceholder}>Loading the player…</div>
                        )}
                      </div>
                      {cycleForPlayer.videoTitle && (
                        <div className={styles.videoTitle}>{cycleForPlayer.videoTitle}</div>
                      )}
                    </div>

                    <aside className={styles.voteSidebar}>
                      <div className={styles.sidebarSection}>
                        <div className={styles.sidebarHeading}>Cycle controls</div>
                        <p className={styles.sessionStatus}>{cycleStatusMessage}</p>
                        <div className={styles.cycleButtons}>
                          <button
                            type="button"
                            className={`${styles.primaryButton} ${styles.cycleAction}`}
                            onClick={handleRevealCycle}
                            disabled={!hasIdentity || !activeCycle || revealLoading || !hasActiveSession}
                          >
                            {revealLoading ? "Revealing…" : "Close voting & reveal"}
                          </button>
                          <button
                            type="button"
                            className={`${styles.secondaryButton} ${styles.cycleAction}`}
                            onClick={handleStartNextCycle}
                            disabled={
                              !hasIdentity ||
                              !!activeCycle ||
                              nextCycleLoading ||
                              availableIngredients === 0 ||
                              !hasActiveSession
                            }
                          >
                            {nextCycleLoading ? "Queuing…" : "Start next video"}
                          </button>
                        </div>
                      </div>

                      <div className={styles.sidebarSection}>
                        <div className={styles.sidebarHeading}>Who’s the curator?</div>
                        <div className={styles.voteSummary}>
                          {activeCycle
                            ? `Votes: ${votesCast}/${totalVotesNeeded}`
                            : hasActiveSession
                              ? "Voting is closed for this video."
                              : "Session is not active."}
                        </div>
                        {displayReveal ? (
                          <>
                            <div className={styles.revealSummary}>
                              <div>
                                <strong>Curator:</strong>{" "}
                                {displayReveal.correctSubmitterIds
                                  .map((hipsterId) => hipsterNameById.get(hipsterId) ?? "Unknown")
                                  .join(", ")}
                              </div>
                              <div>
                                <strong>Correct guessers:</strong>{" "}
                                {displayReveal.correctGuessers.length > 0
                                  ? displayReveal.correctGuessers
                                      .map((hipsterId) => hipsterNameById.get(hipsterId) ?? "Unknown")
                                      .join(", ")
                                  : "No one guessed it this time."}
                              </div>
                            </div>
                            <ul className={styles.tallyList}>
                              {Object.entries(displayReveal.tally)
                                .sort(([, a], [, b]) => b - a)
                                .map(([hipsterId, votes]) => (
                                  <li key={hipsterId} className={styles.tallyRow}>
                                    <span>{hipsterNameById.get(hipsterId) ?? "Unknown"}</span>
                                    <span className={styles.tallyCount}>{votes}</span>
                                  </li>
                                ))}
                            </ul>
                          </>
                        ) : hasIdentity ? (
                          activeCycle ? (
                            <ul className={styles.voteList}>
                              {coffeeBar.hipsters.map((hipster) => (
                                <li key={hipster.id}>
                                  <button
                                    type="button"
                                    className={styles.voteButton}
                                    disabled={
                                      hipster.id === identity?.hipsterId || alreadyVoted || revealLoading
                                    }
                                    onClick={() => handleCastVote(hipster.id)}
                                  >
                                    {hipster.username}
                                  </button>
                                </li>
                              ))}
                            </ul>
                          ) : hasActiveSession ? (
                            <p className={styles.sessionHint}>Voting will resume on the next video.</p>
                          ) : (
                            <p className={styles.sessionHint}>Start a new session to vote again.</p>
                          )
                        ) : (
                          <p className={styles.sessionHint}>Join the bar to cast your vote.</p>
                        )}
                      </div>

                      <div className={styles.sidebarSection}>
                        <div className={styles.sidebarHeading}>Hipsters in the bar</div>
                        <ul className={styles.sidebarHipsterList}>
                          {coffeeBar.hipsters.length === 0 && <li>No hipsters yet. Be the first to join!</li>}
                          {coffeeBar.hipsters.map((hipster) => (
                            <li
                              key={hipster.id}
                              className={identity?.hipsterId === hipster.id ? styles.me : undefined}
                            >
                              <span className={styles.hipsterName}>{hipster.username}</span>
                              <span className={styles.hipsterCount}>{submissionCounts[hipster.id] ?? 0} urls</span>
                            </li>
                          ))}
                        </ul>
                      </div>
                    </aside>
                  </div>
                ) : (
                  <p className={styles.sessionHint}>Start a session to spin up the first video.</p>
                )}
              </section>

              {voteError && <div className={styles.inlineError}>{voteError}</div>}
            </div>
          ) : (
            <div className={styles.leaderboardView}>
              <section className={styles.card}>
                <div className={styles.leaderboardHeader}>
                  <div>
                    <h2 className={styles.cardTitle}>Leaderboard</h2>
                    <p className={styles.leaderboardHint}>
                      Track the sharpest guessers across this bar or drill into a single session.
                    </p>
                  </div>
                  <div className={styles.leaderboardControls}>
                    <label className={styles.leaderboardLabel}>
                      View
                      <select
                        className={styles.leaderboardSelect}
                        value={selectedLeaderboardSessionId}
                        onChange={handleLeaderboardSessionChange}
                      >
                        <option value={OVERALL_LEADERBOARD}>Overall (all sessions)</option>
                        {sortedLeaderboardSessions.map((session) => {
                          const startedAt = new Date(session.startedAt).toLocaleString();
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
                      className={styles.secondaryButton}
                      onClick={handleRefreshLeaderboard}
                      disabled={leaderboardLoading}
                    >
                      {leaderboardLoading ? "Refreshing…" : "Refresh"}
                    </button>
                  </div>
                </div>
                {leaderboardError ? (
                  <div className={styles.inlineError}>{leaderboardError}</div>
                ) : null}
                {activeView === "leaderboard" && leaderboardLoading && !leaderboard ? (
                  <div className={styles.leaderboardStatus}>Loading standings…</div>
                ) : null}
                {myTrendMessage && selectedLeaderboardSessionId === OVERALL_LEADERBOARD ? (
                  <p className={styles.leaderboardTrend}>{myTrendMessage}</p>
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
                      {displayedLeaderboardEntries.map((entry) => {
                        const isMe = identity?.hipsterId === entry.hipsterId;
                        const rowClassName = isMe
                          ? `${styles.leaderboardRow} ${styles.leaderboardMeRow}`
                          : styles.leaderboardRow;

                        return (
                          <tr key={entry.hipsterId} className={rowClassName}>
                            <td className={styles.leaderboardRankCell}>{entry.rank}</td>
                            <td className={styles.leaderboardNameCell}>{entry.username}</td>
                            <td className={styles.leaderboardTrendCell}>{renderTrendIndicator(entry)}</td>
                            <td className={styles.leaderboardScoreCell}>{entry.score}</td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
                {displayedLeaderboardEntries.length === 0 && !leaderboardLoading && !leaderboardError ? (
                  <div className={styles.leaderboardStatus}>No standings yet. Reveal a cycle to see points.</div>
                ) : null}
                {selectedLeaderboardSummary && selectedLeaderboardSessionId !== OVERALL_LEADERBOARD ? (
                  <p className={styles.leaderboardSessionMeta}>{selectedLeaderboardSummary}</p>
                ) : null}
              </section>
            </div>
          )}
        </>
      ) : null}
    </div>
  );
}
