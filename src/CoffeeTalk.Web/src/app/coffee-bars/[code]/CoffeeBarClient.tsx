"use client";

import pageStyles from "@/app/coffee-bars/[code]/CoffeeBarPage.module.css";
import { BarManagementView } from "@/app/coffee-bars/[code]/_components/BarManagementView";
import { CoffeeBarHeader } from "@/app/coffee-bars/[code]/_components/CoffeeBarHeader";
import { CycleView } from "@/app/coffee-bars/[code]/_components/CycleView";
import { LeaderboardView } from "@/app/coffee-bars/[code]/_components/LeaderboardView";
import { useCoffeeBarClient } from "@/app/coffee-bars/[code]/useCoffeeBarClient";

type CoffeeBarClientProps = {
  code: string;
};

export function CoffeeBarClient({ code }: CoffeeBarClientProps) {
  const state = useCoffeeBarClient(code);

  if (state.loading && !state.coffeeBar) {
    return (
      <div className={pageStyles.page}>
        <div className={pageStyles.loading}>Loading your coffee bar…</div>
      </div>
    );
  }

  if (state.error && !state.coffeeBar) {
    return (
      <div className={pageStyles.page}>
        <div className={pageStyles.error}>{state.error}</div>
      </div>
    );
  }

  if (!state.coffeeBar) {
    return null;
  }

  const { coffeeBar } = state;

  return (
    <div className={pageStyles.page}>
      <CoffeeBarHeader
        code={coffeeBar.code}
        theme={coffeeBar.theme}
        shareLink={state.shareLink}
        submissionPolicy={coffeeBar.submissionPolicy}
        maxIngredientsPerHipster={coffeeBar.defaultMaxIngredientsPerHipster}
        activeView={state.activeView}
        onChangeView={state.setActiveView}
      />

      {state.activeView === "bar" && (
        <BarManagementView
          coffeeBar={coffeeBar}
          identity={state.identity}
          hasIdentity={state.hasIdentity}
          sessionState={state.sessionState}
          submissionCounts={state.submissionCounts}
          availableIngredients={state.availableIngredients}
          join={state.join}
          submission={state.submission}
          session={state.session}
          mySubmissions={state.mySubmissions}
          voteError={state.voteError}
          onOpenCycleView={() => state.setActiveView("cycle")}
          submissionsLocked={coffeeBar.submissionsLocked}
        />
      )}

      {state.activeView === "cycle" && (
        <CycleView
          coffeeBar={coffeeBar}
          sessionState={state.sessionState}
          identity={state.identity}
          hasIdentity={state.hasIdentity}
          onBackToBar={() => state.setActiveView("bar")}
          session={state.session}
          cycle={state.cycle}
          availableIngredients={state.availableIngredients}
          isClient={state.isClient}
          submissionCounts={state.submissionCounts}
        />
      )}

      {state.activeView === "leaderboard" && (
        <LeaderboardView
          identity={state.identity}
          leaderboard={state.leaderboardState}
          isActive={state.activeView === "leaderboard"}
        />
      )}
    </div>
  );
}
