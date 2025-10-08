import type { ActiveView } from "../useCoffeeBarClient";
import styles from "../CoffeeBarPage.module.css";

const VIEW_OPTIONS: { view: ActiveView; label: string }[] = [
  { view: "cycle", label: "Cycle view" },
  { view: "bar", label: "Bar management" },
  { view: "leaderboard", label: "Leaderboard" },
];

type ViewSwitcherProps = {
  activeView: ActiveView;
  onChange: (view: ActiveView) => void;
};

export function ViewSwitcher({ activeView, onChange }: ViewSwitcherProps) {
  return (
    <div className={styles.viewSwitcher}>
      {VIEW_OPTIONS.map(({ view, label }) => {
        const isActive = activeView === view;
        const className = isActive
          ? `${styles.viewButton} ${styles.viewButtonActive}`
          : styles.viewButton;

        return (
          <button
            key={view}
            type="button"
            className={className}
            onClick={() => onChange(view)}
          >
            {label}
          </button>
        );
      })}
    </div>
  );
}
