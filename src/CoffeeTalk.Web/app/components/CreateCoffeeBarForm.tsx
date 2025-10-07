"use client";

import { FormEvent, useMemo, useState } from "react";
import styles from "../page.module.css";
import { saveIdentity } from "../lib/identity";

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
  hipsters: HipsterResource[];
  ingredients: IngredientResource[];
  submissions: SubmissionResource[];
};

type CreateCoffeeBarResponse = {
  coffeeBar: CoffeeBarResource;
  hipster: HipsterResource;
};

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/$/, "");
const DEFAULT_QUOTA = 5;

export function CreateCoffeeBarForm() {
  const [theme, setTheme] = useState("");
  const [maxPerHipster, setMaxPerHipster] = useState("");
  const [policy, setPolicy] = useState<SubmissionPolicy>("LockOnFirstBrew");
  const [username, setUsername] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CreateCoffeeBarResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const shareLink = useMemo(() => {
    if (!result || typeof window === "undefined") {
      return "";
    }

    return `${window.location.origin}/coffee-bars/${result.coffeeBar.code}`;
  }, [result]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const quota = maxPerHipster.trim() === "" ? undefined : Number(maxPerHipster);
    if (quota !== undefined && (Number.isNaN(quota) || quota < 1)) {
      setError("Please enter a valid max ingredients value (minimum 1).");
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const response = await fetch(`${API_BASE_URL}/coffee-bars`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          theme,
          defaultMaxIngredientsPerHipster: quota,
          submissionPolicy: policy,
          creatorUsername: username,
        }),
      });

      const payload = await response.json();
      if (!response.ok) {
        setError((payload && (payload.detail ?? payload.title)) || "We couldn't create the coffee bar.");
        return;
      }

      const created = payload as CreateCoffeeBarResponse;
      setResult(created);
      saveIdentity({
        coffeeBarId: created.coffeeBar.id,
        coffeeBarCode: created.coffeeBar.code,
        hipsterId: created.hipster.id,
        username: created.hipster.username,
      });
      setTheme("");
      setMaxPerHipster("");
      setUsername("");
    } catch (err) {
      console.error(err);
      setError("Something went wrong while creating your coffee bar. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className={styles.card}>
      <h2 className={styles.cardTitle}>Create a Coffee Bar</h2>
      <p className={styles.cardSubtitle}>
        Spin up a fresh bar with a shareable code. Everyone can submit their favourite clips once you hand out the code.
      </p>
      <form className={styles.form} onSubmit={handleSubmit}>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="theme">
            Theme or vibe
          </label>
          <input
            id="theme"
            name="theme"
            className={styles.input}
            placeholder="Lo-fi hip hop, synthwave showdown..."
            value={theme}
            onChange={(event) => setTheme(event.target.value)}
            required
            disabled={loading}
          />
        </div>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="maxPerHipster">
            Max ingredients per hipster (optional)
          </label>
          <input
            id="maxPerHipster"
            name="maxPerHipster"
            className={styles.input}
            type="number"
            min={1}
            placeholder={`Default is ${DEFAULT_QUOTA}`}
            value={maxPerHipster}
            onChange={(event) => setMaxPerHipster(event.target.value)}
            disabled={loading}
          />
        </div>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="submissionPolicy">
            Submission policy
          </label>
          <select
            id="submissionPolicy"
            name="submissionPolicy"
            className={styles.select}
            value={policy}
            onChange={(event) => setPolicy(event.target.value as SubmissionPolicy)}
            disabled={loading}
          >
            <option value="LockOnFirstBrew">Lock on first brew</option>
            <option value="AlwaysOpen">Always open</option>
          </select>
        </div>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="creatorUsername">
            Your username
          </label>
          <input
            id="creatorUsername"
            name="creatorUsername"
            className={styles.input}
            placeholder="DJ Espresso"
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
            minLength={3}
            maxLength={20}
            disabled={loading}
          />
        </div>
        <button className={styles.submitButton} type="submit" disabled={loading}>
          {loading ? "Crafting..." : "Create bar"}
        </button>
      </form>
      {result && (
        <div className={styles.result}>
          <strong>Bar ready!</strong>
          <div>Share code: {result.coffeeBar.code}</div>
          <div>
            Theme: <strong>{result.coffeeBar.theme}</strong>
          </div>
          <div>
            Submissions: up to {result.coffeeBar.defaultMaxIngredientsPerHipster} per hipster · policy {result.coffeeBar.submissionPolicy}
          </div>
          {shareLink && (
            <div className={styles.shareLink}>
              Share this link with your crew: <a className={styles.shareAnchor} href={shareLink}>{shareLink}</a>
            </div>
          )}
          <div className={styles.identityHint}>
            You’re in! We saved your handle (<strong>{result.hipster.username}</strong>) so you can dive back into the bar instantly.
          </div>
        </div>
      )}
      {error && <div className={styles.error}>{error}</div>}
    </section>
  );
}
