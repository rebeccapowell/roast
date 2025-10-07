"use client";

import { useActionState, useRef } from "react";
import styles from "../page.module.css";
import {
  createCoffeeBarAction,
  CreateCoffeeBarState,
} from "@/app/_actions/createCoffeeBar";

const DEFAULT_QUOTA = 5;

const initialState: CreateCoffeeBarState = {
  success: false,
};

export const CreateCoffeeBarForm = () => {
  const [state, formAction, isPending] = useActionState(
    createCoffeeBarAction,
    initialState
  );
  const formRef = useRef<HTMLFormElement>(null);

  // Reset form on successful submission
  if (state.success && formRef.current) {
    formRef.current.reset();
  }

  return (
    <section className={styles.card}>
      <h2 className={styles.cardTitle}>Create a Coffee Bar</h2>
      <p className={styles.cardSubtitle}>
        Spin up a fresh bar with a shareable code. Everyone can submit their
        favourite clips once you hand out the code.
      </p>
      <form ref={formRef} className={styles.form} action={formAction}>
        <div className={styles.fieldGroup}>
          <label className={styles.label} htmlFor="theme">
            Theme or vibe
          </label>
          <input
            id="theme"
            name="theme"
            className={styles.input}
            placeholder="Lo-fi hip hop, synthwave showdown..."
            required
            disabled={isPending}
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
            disabled={isPending}
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
            defaultValue="LockOnFirstBrew"
            disabled={isPending}
          >
            <option value="LockOnFirstBrew">Lock on first brew</option>
            <option value="AlwaysOpen">Always open</option>
          </select>
        </div>
        <button
          className={styles.submitButton}
          type="submit"
          disabled={isPending}
        >
          {isPending ? "Crafting..." : "Create bar"}
        </button>
      </form>
      {state.success && state.result && (
        <div className={styles.result}>
          <strong>Bar ready!</strong>
          <div>Share code: {state.result.code}</div>
          <div>
            Theme: <strong>{state.result.theme}</strong>
          </div>
          <div>
            Submissions: up to {state.result.defaultMaxIngredientsPerHipster}{" "}
            per hipster · policy {state.result.submissionPolicy}
          </div>
        </div>
      )}
      {state.error && <div className={styles.error}>{state.error}</div>}
    </section>
  );
};
