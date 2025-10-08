"use client";

import { FormEvent, useMemo, useState } from "react";
import styles from "@/app/page.module.css";
import { saveIdentity } from "@/lib/identity";
import type { JoinCoffeeBarResponse } from "@/types/resources";
import {
  API_BASE_URL,
  USERNAME_MIN_LENGTH,
  USERNAME_MAX_LENGTH,
  COFFEE_BAR_CODE_LENGTH,
  DEFAULT_USERNAME_PLACEHOLDER,
  JSON_HEADERS,
  HTTP_METHODS,
} from "@/constants";

export function JoinCoffeeBarForm() {
  const [code, setCode] = useState("");
  const [username, setUsername] = useState("");
  const [loading, setLoading] = useState(false);
  const [result, setResult] = useState<JoinCoffeeBarResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  const shareLink = useMemo(() => {
    if (!result || typeof window === "undefined") {
      return "";
    }

    return `${window.location.origin}/coffee-bars/${result.coffeeBar.code}`;
  }, [result]);

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    const normalizedCode = code.trim().toUpperCase();
    if (!normalizedCode) {
      setError("Please enter a coffee bar code.");
      return;
    }

    setLoading(true);
    setError(null);
    setResult(null);

    try {
      const joinResponse = await fetch(
        `${API_BASE_URL}/coffee-bars/${normalizedCode}/hipsters`,
        {
          method: HTTP_METHODS.POST,
          headers: JSON_HEADERS,
          body: JSON.stringify({ username }),
        }
      );

      const payload = await joinResponse.json();
      if (joinResponse.status === 404) {
        setError(
          "We couldn't find a coffee bar with that code. Double-check the six characters."
        );
        return;
      }

      if (!joinResponse.ok) {
        setError(
          (payload && (payload.detail ?? payload.title)) ||
            "We couldn't join that coffee bar."
        );
        return;
      }

      const joined = payload as JoinCoffeeBarResponse;
      setResult(joined);
      saveIdentity({
        coffeeBarId: joined.coffeeBar.id,
        coffeeBarCode: joined.coffeeBar.code,
        hipsterId: joined.hipster.id,
        username: joined.hipster.username,
      });
      setUsername("");
    } catch (err) {
      console.error(err);
      setError(
        "Something went wrong while joining the coffee bar. Please try again."
      );
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className={styles.card}>
      <h2 className={styles.cardTitle}>Join a Coffee Bar</h2>
      <p className={styles.cardSubtitle}>
        Enter the bar code you received and choose your handle. Once you’re in
        you can start queuing ingredients.
      </p>
      <form className={styles.form} onSubmit={handleSubmit}>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="code">
            Coffee bar code
          </label>
          <input
            id="code"
            name="code"
            className={styles.input}
            placeholder="e.g. BRW123"
            value={code}
            onChange={(event) => setCode(event.target.value)}
            required
            minLength={COFFEE_BAR_CODE_LENGTH}
            maxLength={COFFEE_BAR_CODE_LENGTH}
            disabled={loading}
          />
        </div>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="username">
            Your username
          </label>
          <input
            id="username"
            name="username"
            className={styles.input}
            placeholder={DEFAULT_USERNAME_PLACEHOLDER}
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            required
            minLength={USERNAME_MIN_LENGTH}
            maxLength={USERNAME_MAX_LENGTH}
            disabled={loading}
          />
        </div>
        <button
          className={styles.submitButton}
          type="submit"
          disabled={loading}
        >
          {loading ? "Joining..." : "Join bar"}
        </button>
      </form>
      {result && (
        <div className={styles.result}>
          <strong>Welcome, {result.hipster.username}!</strong>
          <div>Hipster ID: {result.hipster.id}</div>
          <div>
            You can submit up to {result.hipster.maxIngredientQuota} ingredients
            in {result.coffeeBar.theme}.
          </div>
          <div>Remember this bar code: {result.coffeeBar.code}</div>
          {shareLink && (
            <div className={styles.shareLink}>
              Invite others with this link:{" "}
              <a className={styles.shareAnchor} href={shareLink}>
                {shareLink}
              </a>
            </div>
          )}
          <div className={styles.identityHint}>
            We’ve saved your spot so you can hop straight into the bar next
            time.
          </div>
        </div>
      )}
      {error && <div className={styles.error}>{error}</div>}
    </section>
  );
}
