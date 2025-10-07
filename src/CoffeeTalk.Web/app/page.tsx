import styles from "./page.module.css";
import { CreateCoffeeBarForm } from "@/app/_components/CreateCoffeeBarForm";
import { JoinCoffeeBarForm } from "@/app/_components/JoinCoffeeBarForm";

const Home = () => {
  return (
    <div className={styles.page}>
      <header className={styles.header}>Coffee-Time ☕🎶</header>
      <div className={styles.tagline}>Brew your beats. Sip your style.</div>
      <div className={styles.cards}>
        <CreateCoffeeBarForm />
        <JoinCoffeeBarForm />
      </div>
      <footer className={styles.footer}>
        Hackweek #7 2025 · Made with ☕ by ROAST Hackweek Team
      </footer>
    </div>
  );
};

export default Home;
