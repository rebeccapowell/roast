"use client";

import { FormEvent, useState } from "react";
import styles from "../page.module.css";

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
};

const API_BASE_URL = (process.env.NEXT_PUBLIC_API_BASE_URL ?? "").replace(/\/$/, "");
const DEFAULT_QUOTA = 5;

export function CreateCoffeeBarForm() {
  const [theme, setTheme] = useState("");
  const [maxPerHipster, setMaxPerHipster] = useState("");
  const [policy, setPolicy] = useState<SubmissionPolicy>("LockOnFirstBrew");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<CoffeeBarResource | null>(null);
  const [error, setError] = useState<string | null>(null);

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
        }),
      });

      const payload = await response.json();
      if (!response.ok) {
        setError((payload && (payload.detail ?? payload.title)) || "We couldn't create the coffee bar.");
        return;
      }

      setResult(payload as CoffeeBarResource);
      setTheme("");
      setMaxPerHipster("");
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
        <button className={styles.submitButton} type="submit" disabled={loading}>
          {loading ? "Crafting..." : "Create bar"}
        </button>
      </form>
      {result && (
        <div className={styles.result}>
          <strong>Bar ready!</strong>
          <div>Share code: {result.code}</div>
          <div>
            Theme: <strong>{result.theme}</strong>
          </div>
          <div>
            Submissions: up to {result.defaultMaxIngredientsPerHipster} per hipster · policy {result.submissionPolicy}
          </div>
        </div>
      )}
      {error && <div className={styles.error}>{error}</div>}
    </section>
  );
}
