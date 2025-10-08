import type { SubmissionPolicy } from "../types";
import type { ActiveView } from "../useCoffeeBarClient";
import pageStyles from "../CoffeeBarPage.module.css";
import sharedStyles from "../CoffeeBarShared.module.css";
import { ViewSwitcher } from "./ViewSwitcher";

type CoffeeBarHeaderProps = {
  code: string;
  theme: string;
  shareLink: string;
  submissionPolicy: SubmissionPolicy;
  maxIngredientsPerHipster: number;
  activeView: ActiveView;
  onChangeView: (view: ActiveView) => void;
};

export function CoffeeBarHeader({
  code,
  theme,
  shareLink,
  submissionPolicy,
  maxIngredientsPerHipster,
  activeView,
  onChangeView,
}: CoffeeBarHeaderProps) {
  const policyLabel = submissionPolicy === "LockOnFirstBrew" ? "Lock on first brew" : "Always open";

  return (
    <header className={pageStyles.header}>
      <div className={pageStyles.headerInfo}>
        <div className={pageStyles.headerTop}>Coffee Bar · {code}</div>
        <h1 className={pageStyles.title}>{theme}</h1>
      </div>
      <div className={pageStyles.headerActions}>
        <p className={pageStyles.shareHint}>
          Share this link with your crew:{" "}
          {shareLink ? (
            <a className={sharedStyles.shareAnchor} href={shareLink}>
              {shareLink}
            </a>
          ) : (
            "Copy the URL"
          )}
        </p>
        <p className={pageStyles.policy}>
          Policy: <strong>{policyLabel}</strong>
          {" · "}Max ingredients per hipster: <strong>{maxIngredientsPerHipster}</strong>
        </p>
        <ViewSwitcher activeView={activeView} onChange={onChangeView} />
      </div>
    </header>
  );
}
