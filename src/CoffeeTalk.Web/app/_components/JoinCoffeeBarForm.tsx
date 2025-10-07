"use client";

import { useActionState, useRef } from "react";
import styles from "../page.module.css";
import {
  joinCoffeeBarAction,
  JoinCoffeeBarState,
} from "@/app/_actions/joinCoffeeBar";

const initialState: JoinCoffeeBarState = {
  success: false,
};

export const JoinCoffeeBarForm = () => {
  const [state, formAction, isPending] = useActionState(
    joinCoffeeBarAction,
    initialState
  );
  const formRef = useRef<HTMLFormElement>(null);

  // Reset form on successful submission
  if (state.success && formRef.current) {
    formRef.current.reset();
  }

  return (
    <section className={styles.card}>
      <h2 className={styles.cardTitle}>Join a Coffee Bar</h2>
      <p className={styles.cardSubtitle}>
        Enter the bar code you received and choose your handle. Once you&apos;re
        in you can start queuing ingredients.
      </p>
      <form ref={formRef} className={styles.form} action={formAction}>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="code">
            Coffee bar code
          </label>
          <input
            id="code"
            name="code"
            className={styles.input}
            placeholder="e.g. BRW123"
            required
            minLength={6}
            maxLength={6}
            disabled={isPending}
            style={{ textTransform: "uppercase" }}
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
            placeholder="DJ Espresso"
            required
            minLength={3}
            maxLength={20}
            disabled={isPending}
          />
        </div>
        <button
          className={styles.submitButton}
          type="submit"
          disabled={isPending}
        >
          {isPending ? "Joining..." : "Join bar"}
        </button>
      </form>
      {state.success && state.result && (
        <div className={styles.result}>
          <strong>Welcome, {state.result.hipster.username}!</strong>
          <div>Hipster ID: {state.result.hipster.id}</div>
          <div>
            You can submit up to {state.result.hipster.maxIngredientQuota}{" "}
            ingredients in {state.result.coffeeBar.theme}.
          </div>
          <div>Remember this bar code: {state.result.coffeeBar.code}</div>
        </div>
      )}
      {state.error && <div className={styles.error}>{state.error}</div>}
    </section>
  );
};
