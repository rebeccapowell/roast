"use client";

import type { HubConnection } from "@microsoft/signalr";
import {
  ChangeEvent,
  FormEvent,
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";

import {
  getIdentity,
  removeIdentity,
  saveIdentity,
  type HipsterIdentity,
} from "@/lib/identity";
import type {
  BrewCycleResource,
  CoffeeBarLeaderboardResource,
  CoffeeBarResource,
  IngredientResource,
  JoinCoffeeBarResponse,
  LeaderboardEntryResource,
  RevealCycleResponse,
  RevealResultResource,
  SessionLeaderboardResource,
  SessionStateResource,
  SubmissionResource,
  SubmitIngredientResponse,
} from "@/app/coffee-bars/[code]/types";

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(
  /\/$/,
  ""
);

export const OVERALL_LEADERBOARD = "overall" as const;

export type ActiveView = "bar" | "cycle" | "leaderboard";

export type UseCoffeeBarClientResult = {
  loading: boolean;
  error: string | null;
  coffeeBar: CoffeeBarResource | null;
  sessionState: SessionStateResource | null;
  identity: HipsterIdentity | null;
  activeView: ActiveView;
  setActiveView: (view: ActiveView) => void;
  shareLink: string;
  realtimeConnected: boolean;
  isClient: boolean;
  hasIdentity: boolean;
  availableIngredients: number;
  submissionCounts: Record<string, number>;
  mySubmissions: {
    submission: SubmissionResource;
    ingredient?: IngredientResource;
  }[];
  join: {
    username: string;
    setUsername: (value: string) => void;
    loading: boolean;
    error: string | null;
    handleSubmit: (event: FormEvent<HTMLFormElement>) => void;
  };
  submission: {
    url: string;
    setUrl: (value: string) => void;
    loading: boolean;
    handleSubmit: (event: FormEvent<HTMLFormElement>) => void;
    handleRemove: (submissionId: string) => void;
  };
  session: {
    loading: boolean;
    ending: boolean;
    start: () => void;
    end: () => void;
    hasActiveSession: boolean;
    activeCycle: BrewCycleResource | null;
    latestCycle: BrewCycleResource | null;
    endedAt: string | null;
    statusMessage: string;
    cycleForPlayer: BrewCycleResource | null;
  };
  cycle: {
    totalVotesNeeded: number;
    votesCast: number;
    alreadyVoted: boolean;
    revealLoading: boolean;
    nextCycleLoading: boolean;
    displayReveal: RevealResultResource | null;
    hipsterNameById: Map<string, string>;
    handleVote: (hipsterId: string) => void;
    handleReveal: () => void;
    handleStartNextCycle: () => void;
    voteError: string | null;
  };
  leaderboardState: {
    leaderboard: CoffeeBarLeaderboardResource | null;
    loading: boolean;
    error: string | null;
    selectedSessionId: string;
    setSelectedSessionId: (value: string) => void;
    sortedSessions: SessionLeaderboardResource[];
    displayedEntries: LeaderboardEntryResource[];
    showTrendColumn: boolean;
    myTrendMessage: string | null;
    selectedSessionSummary: string | null;
    refresh: () => void;
    handleSessionChange: (event: ChangeEvent<HTMLSelectElement>) => void;
  };
  voteError: string | null;
};

export function useCoffeeBarClient(code: string): UseCoffeeBarClientResult {
  const normalizedCode = code.toUpperCase();

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [coffeeBar, setCoffeeBar] = useState<CoffeeBarResource | null>(null);
  const [sessionState, setSessionState] = useState<SessionStateResource | null>(
    null
  );
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
  const [activeView, setActiveView] = useState<ActiveView>("bar");
  const [revealResult, setRevealResult] = useState<RevealResultResource | null>(
    null
  );
  const [lastCycleId, setLastCycleId] = useState<string | null>(null);
  const [realtimeConnected, setRealtimeConnected] = useState(false);
  const [playerCycle, setPlayerCycle] = useState<BrewCycleResource | null>(
    null
  );
  const [isClient, setIsClient] = useState(false);
  const [leaderboard, setLeaderboard] =
    useState<CoffeeBarLeaderboardResource | null>(null);
  const [leaderboardLoading, setLeaderboardLoading] = useState(false);
  const [leaderboardError, setLeaderboardError] = useState<string | null>(null);
  const [leaderboardStale, setLeaderboardStale] = useState(true);
  const [selectedLeaderboardSessionId, setSelectedLeaderboardSessionId] =
    useState<string>(OVERALL_LEADERBOARD);

  const loadIdentity = useCallback(() => {
    const stored = getIdentity(normalizedCode);
    if (stored) {
      setIdentity(stored);
    }
  }, [normalizedCode]);

  const requestCoffeeBar = useCallback(async () => {
    const response = await fetch(
      `${API_BASE_URL}/coffee-bars/${normalizedCode}`
    );
    if (!response.ok) {
      if (response.status === 404) {
        throw new Error(
          "We couldn't find this coffee bar. Check the code and try again."
        );
      }

      const payload = await response.json().catch(() => null);
      throw new Error(
        (payload && (payload.detail ?? payload.title)) ||
          "Unable to load this coffee bar."
      );
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
      setError(
        err instanceof Error ? err.message : "Unable to load this coffee bar."
      );
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
      const response = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/leaderboard`
      );
      if (!response.ok) {
        const payload = await response.json().catch(() => null);
        throw new Error(
          (payload && (payload.detail ?? payload.title)) ||
            "Unable to load leaderboard standings."
        );
      }

      const payload = (await response.json()) as CoffeeBarLeaderboardResource;
      setLeaderboard(payload);
    } catch (err) {
      console.error(err);
      setLeaderboardError(
        err instanceof Error
          ? err.message
          : "Unable to load leaderboard standings."
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
    if (
      activeView !== "leaderboard" ||
      !leaderboardStale ||
      leaderboardLoading
    ) {
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
      !leaderboard.sessions.some(
        (session) => session.sessionId === selectedLeaderboardSessionId
      )
    ) {
      setSelectedLeaderboardSessionId(OVERALL_LEADERBOARD);
    }
  }, [leaderboard, selectedLeaderboardSessionId]);

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
      setSessionState((current) =>
        current ? { ...current, coffeeBar: resource } : current
      );
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

    const stillPresent = coffeeBar.hipsters.some(
      (hipster) => hipster.id === identity.hipsterId
    );
    if (!stillPresent) {
      removeIdentity(coffeeBar.code);
      setIdentity(null);
    }
  }, [coffeeBar, identity]);

  const ingredientById = useMemo(() => {
    if (!coffeeBar) {
      return new Map<string, IngredientResource>();
    }

    return new Map(
      coffeeBar.ingredients.map(
        (ingredient) => [ingredient.id, ingredient] as const
      )
    );
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

    return coffeeBar.ingredients.filter((ingredient) => !ingredient.isConsumed)
      .length;
  }, [coffeeBar]);

  const sessionEndedAt = sessionState?.session.endedAt ?? null;
  const hasActiveSession = Boolean(sessionState && !sessionEndedAt);

  const sessionCycles = useMemo(
    () => sessionState?.session.cycles ?? [],
    [sessionState]
  );

  const activeCycle = useMemo(() => {
    if (!hasActiveSession) {
      return null;
    }

    return sessionCycles.find((cycle) => cycle.revealedAt === null) ?? null;
  }, [hasActiveSession, sessionCycles]);

  const latestCycle = useMemo(
    () =>
      sessionCycles.length ? sessionCycles[sessionCycles.length - 1] : null,
    [sessionCycles]
  );

  useEffect(() => {
    if (latestCycle) {
      setPlayerCycle((current) =>
        current && current.id === latestCycle.id ? current : latestCycle
      );
    } else if (!sessionState) {
      setPlayerCycle(null);
    }
  }, [latestCycle, sessionState]);

  const cycleForPlayer = playerCycle ?? activeCycle ?? latestCycle;

  const hipsterNameById = useMemo(() => {
    if (!coffeeBar) {
      return new Map<string, string>();
    }

    return new Map(
      coffeeBar.hipsters.map(
        (hipster) => [hipster.id, hipster.username] as const
      )
    );
  }, [coffeeBar]);

  const sortedLeaderboardSessions = useMemo(() => {
    if (!leaderboard) {
      return [] as SessionLeaderboardResource[];
    }

    return [...leaderboard.sessions].sort(
      (a, b) =>
        new Date(b.startedAt).getTime() - new Date(a.startedAt).getTime()
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
      leaderboard.sessions.find(
        (session) => session.sessionId === selectedLeaderboardSessionId
      )?.entries ?? []
    );
  }, [leaderboard, selectedLeaderboardSessionId]);

  const selectedLeaderboardSession = useMemo(() => {
    if (!leaderboard || selectedLeaderboardSessionId === OVERALL_LEADERBOARD) {
      return null;
    }

    return (
      leaderboard.sessions.find(
        (session) => session.sessionId === selectedLeaderboardSessionId
      ) ?? null
    );
  }, [leaderboard, selectedLeaderboardSessionId]);

  const showTrendColumn = selectedLeaderboardSessionId === OVERALL_LEADERBOARD;

  const myLeaderboardEntry = useMemo(() => {
    if (!identity || !leaderboard) {
      return null;
    }

    return (
      leaderboard.overall.find(
        (entry) => entry.hipsterId === identity.hipsterId
      ) ?? null
    );
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
          ? `You're climbing the leaderboard (up ${change} place${
              change === 1 ? "" : "s"
            }). Keep it brewing!`
          : "You're climbing the leaderboard. Keep it brewing!";
      }
      case "down": {
        const change = myLeaderboardEntry.previousRank
          ? myLeaderboardEntry.rank - myLeaderboardEntry.previousRank
          : 0;
        return change > 0
          ? `You're slipping ${change} place${
              change === 1 ? "" : "s"
            } on the leaderboard. Time for a comeback.`
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

    const started = new Date(
      selectedLeaderboardSession.startedAt
    ).toLocaleString();
    if (selectedLeaderboardSession.endedAt) {
      const ended = new Date(
        selectedLeaderboardSession.endedAt
      ).toLocaleString();
      return `Session ran from ${started} to ${ended}.`;
    }

    return `Session started ${started} and is still active.`;
  }, [selectedLeaderboardSession]);

  const handleLeaderboardSessionChange = useCallback(
    (event: ChangeEvent<HTMLSelectElement>) => {
      setSelectedLeaderboardSessionId(event.target.value);
    },
    []
  );

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

  const totalVotesNeeded = hasActiveSession
    ? coffeeBar?.hipsters.length ?? 0
    : 0;
  const votesCast = activeCycle?.votes.length ?? 0;
  const hasIdentity = Boolean(identity);
  const alreadyVoted = Boolean(
    identity &&
      activeCycle &&
      activeCycle.votes.some(
        (vote) => vote.voterHipsterId === identity.hipsterId
      )
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
        const response = await fetch(
          `${API_BASE_URL}/coffee-bars/${normalizedCode}/hipsters`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username: trimmed }),
          }
        );

        const payload = await response.json();
        if (!response.ok) {
          setJoinError(
            (payload && (payload.detail ?? payload.title)) ||
              "We couldn't join that coffee bar."
          );
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
    [joinUsername, normalizedCode]
  );

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
        const response = await fetch(
          `${API_BASE_URL}/coffee-bars/${normalizedCode}/ingredients`,
          {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
              hipsterId: identity.hipsterId,
              url: trimmed,
            }),
          }
        );

        const payload = await response.json();
        if (!response.ok) {
          setVoteError(
            (payload && (payload.detail ?? payload.title)) ||
              "We couldn't submit that URL."
          );
          return;
        }

        const submission = payload as SubmitIngredientResponse;
        setCoffeeBar(submission.coffeeBar);
        setSessionState((current) =>
          current ? { ...current, coffeeBar: submission.coffeeBar } : current
        );
        setUrlInput("");
      } catch (err) {
        console.error(err);
        setVoteError(
          "Something went wrong while submitting. Please try again."
        );
      } finally {
        setSubmissionLoading(false);
      }
    },
    [identity, normalizedCode, urlInput]
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
          }
        );

        if (response.status === 404) {
          setVoteError("We couldn't find that submission anymore.");
          await refreshCoffeeBar();
          return;
        }

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
          setVoteError(
            (payload && (payload.detail ?? payload.title)) ||
              "We couldn't remove that submission."
          );
          return;
        }

        const updated = payload as CoffeeBarResource;
        setCoffeeBar(updated);
        setSessionState((current) =>
          current ? { ...current, coffeeBar: updated } : current
        );
      } catch (err) {
        console.error(err);
        setVoteError("Something went wrong while removing the URL.");
      }
    },
    [identity, normalizedCode, refreshCoffeeBar]
  );

  const refreshSession = useCallback(
    async (sessionId: string) => {
      try {
        const response = await fetch(
          `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions/${sessionId}`
        );
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
    [normalizedCode]
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
  }, [
    coffeeBar,
    hasActiveSession,
    realtimeConnected,
    refreshCoffeeBar,
    refreshSession,
    sessionState,
  ]);

  useEffect(() => {
    if (!sessionState) {
      setActiveView("bar");
      setRevealResult(null);
      setLastCycleId(null);
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
  }, [sessionState, latestCycle, lastCycleId]);

  const handleStartSession = useCallback(async () => {
    if (!coffeeBar || hasActiveSession) {
      return;
    }

    setSessionLoading(true);
    setVoteError(null);

    try {
      const response = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/sessions`,
        {
          method: "POST",
        }
      );

      const payload = await response.json();
      if (!response.ok) {
        setVoteError(
          (payload && (payload.detail ?? payload.title)) ||
            "We couldn't start the session."
        );
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
        { method: "POST" }
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError(
          (payload && (payload.detail ?? payload.title)) ||
            "We couldn't stop the session."
        );
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
            body: JSON.stringify({
              voterHipsterId: identity.hipsterId,
              targetHipsterId,
            }),
          }
        );

        const payload = await response.json();
        if (!response.ok) {
          setVoteError(
            (payload && (payload.detail ?? payload.title)) ||
              "We couldn't cast that vote."
          );
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
    [identity, activeCycle, normalizedCode]
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
        }
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError(
          (payload && (payload.detail ?? payload.title)) ||
            "We couldn't reveal this cycle."
        );
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
        }
      );

      const payload = await response.json().catch(() => null);
      if (!response.ok) {
        setVoteError(
          (payload && (payload.detail ?? payload.title)) ||
            "We couldn't start the next cycle."
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
      return [] as {
        submission: SubmissionResource;
        ingredient?: IngredientResource;
      }[];
    }

    return coffeeBar.submissions
      .filter((submission) => submission.hipsterId === identity.hipsterId)
      .map((submission) => ({
        submission,
        ingredient: ingredientById.get(submission.ingredientId),
      }));
  }, [coffeeBar, identity, ingredientById]);

  const totalState = {
    loading,
    error,
    coffeeBar,
    sessionState,
    identity,
    activeView,
    setActiveView,
    shareLink,
    realtimeConnected,
    isClient,
    hasIdentity,
    availableIngredients,
    submissionCounts,
    mySubmissions,
    join: {
      username: joinUsername,
      setUsername: setJoinUsername,
      loading: joinLoading,
      error: joinError,
      handleSubmit: handleJoin,
    },
    submission: {
      url: urlInput,
      setUrl: setUrlInput,
      loading: submissionLoading,
      handleSubmit: handleSubmitIngredient,
      handleRemove: handleRemoveSubmission,
    },
    session: {
      loading: sessionLoading,
      ending: endSessionLoading,
      start: handleStartSession,
      end: handleEndSession,
      hasActiveSession,
      activeCycle,
      latestCycle,
      endedAt: sessionEndedAt,
      statusMessage: cycleStatusMessage,
      cycleForPlayer,
    },
    cycle: {
      totalVotesNeeded,
      votesCast,
      alreadyVoted,
      revealLoading,
      nextCycleLoading,
      displayReveal,
      hipsterNameById,
      handleVote: handleCastVote,
      handleReveal: handleRevealCycle,
      handleStartNextCycle,
      voteError,
    },
    leaderboardState: {
      leaderboard,
      loading: leaderboardLoading,
      error: leaderboardError,
      selectedSessionId: selectedLeaderboardSessionId,
      setSelectedSessionId: setSelectedLeaderboardSessionId,
      sortedSessions: sortedLeaderboardSessions,
      displayedEntries: displayedLeaderboardEntries,
      showTrendColumn,
      myTrendMessage,
      selectedSessionSummary: selectedLeaderboardSummary,
      refresh: handleRefreshLeaderboard,
      handleSessionChange: handleLeaderboardSessionChange,
    },
    voteError,
  } satisfies UseCoffeeBarClientResult;

  return totalState;
}
