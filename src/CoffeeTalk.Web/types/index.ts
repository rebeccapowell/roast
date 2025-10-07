export interface HipsterResource {
  id: string;
  username: string;
  maxIngredientQuota: number;
}

export interface IngredientResource {
  id: string;
  videoId: string;
  isConsumed: boolean;
  submitterIds: string[];
}

export interface CoffeeBarResource {
  id: string;
  code: string;
  theme: string;
  defaultMaxIngredientsPerHipster: number;
  submissionPolicy: SubmissionPolicy;
  submissionsLocked: boolean;
  isClosed: boolean;
  hipsters: HipsterResource[];
  ingredients: IngredientResource[];
}

export interface JoinCoffeeBarResponse {
  coffeeBar: CoffeeBarResource;
  hipster: HipsterResource;
}

export type SubmissionPolicy = "LockOnFirstBrew" | "AlwaysOpen";
