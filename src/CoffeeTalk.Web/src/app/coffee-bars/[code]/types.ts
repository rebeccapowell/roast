export type SubmissionPolicy = "LockOnFirstBrew" | "AlwaysOpen";

export type HipsterResource = {
  id: string;
  username: string;
  maxIngredientQuota: number;
};

export type IngredientResource = {
  id: string;
  videoId: string;
  isConsumed: boolean;
  submitterIds: string[];
  title: string | null;
  thumbnailUrl: string | null;
};

export type SubmissionResource = {
  id: string;
  ingredientId: string;
  hipsterId: string;
  submittedAt: string;
};

export type CoffeeBarResource = {
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

export type SubmitIngredientResponse = {
  coffeeBar: CoffeeBarResource;
  ingredient: IngredientResource;
  submissionId: string;
};

export type JoinCoffeeBarResponse = {
  coffeeBar: CoffeeBarResource;
  hipster: HipsterResource;
};

export type VoteResource = {
  id: string;
  voterHipsterId: string;
  targetHipsterId: string;
  castAt: string;
  isCorrect: boolean | null;
};

export type BrewCycleResource = {
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

export type BrewSessionResource = {
  id: string;
  startedAt: string;
  endedAt: string | null;
  cycles: BrewCycleResource[];
};

export type SessionStateResource = {
  coffeeBar: CoffeeBarResource;
  session: BrewSessionResource;
};

export type RevealResultResource = {
  cycleId: string;
  tally: Record<string, number>;
  correctSubmitterIds: string[];
  correctGuessers: string[];
};

export type RevealCycleResponse = {
  session: SessionStateResource;
  reveal: RevealResultResource;
};

export type LeaderboardTrend = "stable" | "up" | "down";

export type LeaderboardEntryResource = {
  hipsterId: string;
  username: string;
  score: number;
  rank: number;
  previousRank: number | null;
  trend: LeaderboardTrend;
};

export type SessionLeaderboardResource = {
  sessionId: string;
  startedAt: string;
  endedAt: string | null;
  entries: LeaderboardEntryResource[];
};

export type CoffeeBarLeaderboardResource = {
  overall: LeaderboardEntryResource[];
  sessions: SessionLeaderboardResource[];
};
