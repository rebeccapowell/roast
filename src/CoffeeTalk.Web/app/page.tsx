import styles from "./page.module.css";

export default function Home() {
  return (
    <div className={styles.page}>
      <header className={styles.header}>Coffee-Time ☕🎶</header>
      <div className={styles.tagline}>Brew your beats. Sip your style.</div>
      <div className={styles.buttonContainer}>
        <button className={`${styles.button} ${styles.createButton}`} type="button">
          Create a Coffee Bar
        </button>
        <button className={`${styles.button} ${styles.joinButton}`} type="button">
          Join a Coffee Bar
        </button>
      </div>
      <footer className={styles.footer}>
        Hackweek #7 2025 · Made with ☕ by ROAST Hackweek Team
      </footer>
    </div>
  );
}
