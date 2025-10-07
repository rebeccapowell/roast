# Coffee Talk App — Product Requirements Document (PoC / Hackweek)
**Date:** 2025-10-07  
**Stack:** .NET 9 (API + SignalR), Postgres 16, Next.js 15 (App Router), Aspire 9.0.5  
**Realtime:** SignalR (WebSockets preferred, fallback allowed)  
**Telemetry:** OpenTelemetry via Aspire  
**Scope:** Small PoC (≈100 Coffee Bars; up to 25 Hipsters per bar)

---


## 1. Product Vision & Summary
A lightweight web app for groups to submit YouTube links (called **Ingredients**) to a shared **Coffee Bar** (room) and then run live “guess‑who-submitted-it” rounds (**Brew Cycles**). Everyone sees the same video playback (synchronized via SignalR), casts a vote (**Bean**) guessing which **Hipster** (user) submitted the video, and then the reveal shows who was right with a Mentimeter‑style bar chart.

### Key Principles
- **Frictionless join**: join with room code + username (no accounts).
- **Flexible submission policy**: creator config chooses whether submissions lock at first session start or remain open.
- **Duplicates allowed**: multiple Hipsters can submit the same video; the app shows that video **once**, and a guess is “correct” if it matches **any submitter** of that video.
- **No moderation in PoC**: everyone is Barista (can control round flow); no observers/read‑only mode.
- **Hackweek-ready**: minimal infra, easy dev loop, strong defaults.

---

## 2. In-Scope vs Out-of-Scope (PoC)
### In-Scope
- Create Coffee Bars with code (6 chars A–Z0–9, consonants only; case-insensitive), theme/topic, default **max Ingredients per Hipster** (default 5; creator can change).
- Join via room code + username (unique per bar; 3–20 chars; case-insensitive).
- Submit YouTube links (normalized to **video ID**). Duplicates allowed; display video **once**.
- Start session (“brew”). Round flow is **manual advance** (anyone can click **Next**).
- Synchronized playback via YouTube IFrame API, start/stop broadcasts via SignalR.
- Voting: each present Hipster casts one Bean; **cannot vote for yourself**. Correct if guessed **any submitter** tied to the video.
- Reveal: Mentimeter-style bar chart (x-axis Hipsters in the bar; y-axis vote counts). Show a tick mark next to the correct target(s) and a sidebar listing who got it right.
- Session lifecycle: Coffee Bar can span **multiple sessions**; closes permanently when **all videos are exhausted**.
- Leaderboards: long-term per Coffee Bar and a cross-bar leaderboard.
- Telemetry: traces, metrics, logs via OTel; Aspire wiring.
- CI: GitHub Actions (build, unit tests, minimal API tests, Playwright e2e).

### Out-of-Scope (for this PoC)
- User accounts / OAuth.
- Observers/spectators.
- Moderation (kicks/locks/chat).  
- Profanity filter.  
- Rate limiting (join/submission).  
- Backplane/Redis scale-out (single node only).  
- Content filtering (accept all YouTube).

> **Note on YouTube Ads:** We cannot suppress ads. Provide a **“Resync”** button to re-align clients after ads.

---

## 3. Roles & Permissions
- **Hipster**: any participant; can join, submit, vote, and (in PoC) act as **Barista**.
- **Barista**: for PoC, **everyone is Barista** (start/stop video, reveal votes, next round).

---

## 4. Core Concepts & Definitions
- **Coffee Bar**: a room identified by a short code (6 chars A–Z0–9, no vowels), theme/topic, and configuration.
- **Hipster**: participant with a unique username within the Coffee Bar.
- **Ingredient**: a YouTube video submission, stored by video ID; duplicates allowed (kept as multiple **Submissions** referencing the same **Ingredient**).
- **Brew Session**: a run of one or more rounds; a Coffee Bar can have multiple sessions.
- **Brew Cycle (Round)**: choose an Ingredient not yet used; show the video; collect votes; reveal.
- **Bean (Vote)**: a guess mapping voter → target Hipster. A vote is correct if the target Hipster is **any** submitter of the Ingredient.

---

## 5. User Journeys (Condensed)
1) **Create Bar** → theme/topic, choose submission policy (lock at first brew vs. always open), set default max Ingredients per Hipster (default 5) → receive room code → share join URL with code.
2) **Join Bar** → enter code & username → land in lobby → (optionally) submit Ingredients (YouTube links).
3) **Brew** → any user clicks **Start** → system selects next unused Ingredient (random) → broadcast YouTube video → Hipsters vote → Barista clicks **Reveal** → chart displayed → **Next** or **End**.
4) **Close** → when no remaining Ingredients, Coffee Bar closes and shows historical leaderboards.

---

## 6. UX & UI Requirements
### 6.1 Screens
- **Landing / Join**: input room code; deep-link support `/?code=ABC123`.
- **Lobby**: theme/topic, submission policy, list of Hipsters present, your quota status, list of Ingredients (count only, titles hidden by default), button: **Start Brew**.
- **Round View**: synchronized player pane + right-side vote panel (select Hipster; self is disabled). Footer shows status: “X/Y voted” and **Stop**, **Reveal**, **Next**.
- **Reveal View**: bar chart (x: all Hipsters in room, y: votes). A **✓** tick indicates the correct submitter(s). Sidebar lists the voters who guessed correctly.
- **Leaderboards**: per Coffee Bar and global (cross-bar) summary.
- **Empty State**: if no Ingredients remain, show close message & statistics.

### 6.2 Charts
- **Mentimeter-style** bar chart:
  - X-axis: usernames of all Hipsters currently in the Coffee Bar.
  - Y-axis: number of votes received this round.
  - Mark **✓** over the bar(s) for correct submitter(s).
  - Sidebar: a scrollable list “Correct Guessers” (usernames).

### 6.3 Responsiveness
- **Desktop web only** (PoC). Mobile web not required.

---

## 7. Functional Requirements
### 7.1 Coffee Bar Creation
- Code: 6 chars, A–Z0–9, **no vowels**, case-insensitive.
- Inputs: theme/topic (free text), default max Ingredients per Hipster (default 5), submission policy:
  - **Lock-on-first-brew**: after the first session starts, no new submissions.
  - **Always-open**: submissions allowed anytime; only the **currently playing** round is locked.
- Persistence: bars do **not** expire; results retained indefinitely.

### 7.2 Joining & Presence
- Provide username (3–20 chars, case-insensitive), unique within the bar.
- Presence & active Hipsters: track connected clients; show **present Hipsters** list in UI.
- Participation: Hipsters may join and vote even if they did **not** submit any videos.

### 7.3 Submissions
- Accept YouTube URLs; normalize to **video ID**.
- Allow duplicates; store **Submission** (Hipster, Ingredient, timestamp).
- “Display once” rule: during Brew Cycles, each **Ingredient** is queued once (even if multiple submissions exist).
- If an Ingredient has multiple submitters, **all** are considered correct targets for that round’s video.

### 7.4 Round Flow
- Randomly select an **unused Ingredient**.
- Broadcast **Start** (YouTube IFrame API cue + play).
- Voting window opens; exactly **one** vote per present Hipster.
- **Self-vote disabled** (you cannot vote for yourself).
- Barista actions (anyone): **Stop**, **Reveal**, **Next** (manual advance).
- Auto-complete is **not** required; we advance only on **Next**.

### 7.5 Reveal & Scoring
- Reveal shows bar chart + correct submitter(s) tick.
- A vote is **correct** if voter’s target is any submitter of the Ingredient.
- Points: 1 point per correct guess for the voter.
- Submitter points: **none** in PoC (can be a future toggle).
- Leaderboards:
  - **Per Coffee Bar**: cumulative correct guesses per Hipster.
  - **Global**: across all bars (sum of per-bar scores).

### 7.6 Closing
- When all Ingredients have been used in at least one round, the Coffee Bar **closes**. Historical stats remain viewable.

---

## 8. Non-Functional Requirements
- **Scale (PoC)**: ≈100 bars total; up to 25 Hipsters/bar.
- **Latency**: <250ms for round control events; video sync inherently best-effort.
- **Resilience**: single-node (no Redis backplane). Safe restart behavior (rejoin restores state).
- **Security**: code treated as secret; no rate limits in PoC; no profanity filter.
- **Privacy/GDPR**: pseudonyms only; store in EU region. No export/delete endpoints in PoC.
- **Logging/Tracing**: OTel signals via Aspire; include correlation IDs in hub messages.

---

## 9. Data Model (ERD)
```mermaid
erDiagram
    CoffeeBar ||--o{ Hipster : has
    CoffeeBar ||--o{ Submission : allows
    CoffeeBar ||--o{ BrewSession : runs
    BrewSession ||--o{ BrewCycle : contains
    Ingredient ||--o{ Submission : isReferencedBy
    BrewCycle ||--o{ Vote : collects
    Hipster ||--o{ Vote : casts
    Ingredient ||--o{ BrewCycle : appearsIn

    CoffeeBar {
      uuid id
      string code
      string theme
      string submissionPolicy  // LockOnFirstBrew | AlwaysOpen
      int defaultMaxPerHipster // default 5
      timestamp createdAt
    }

    Hipster {
      uuid id
      uuid coffeeBarId
      string username
      timestamp joinedAt
      bool isPresent
      int score // cumulative correct votes
    }

    Ingredient {
      uuid id
      string youtubeVideoId
      string title
      int durationSec
      timestamp createdAt
      bool exhausted // once played in a brew cycle
    }

    Submission {
      uuid id
      uuid coffeeBarId
      uuid hipsterId
      uuid ingredientId
      timestamp submittedAt
    }

    BrewSession {
      uuid id
      uuid coffeeBarId
      timestamp startedAt
      timestamp endedAt
    }

    BrewCycle {
      uuid id
      uuid brewSessionId
      uuid ingredientId
      timestamp startedAt
      timestamp revealedAt
      timestamp endedAt
    }

    Vote {
      uuid id
      uuid brewCycleId
      uuid voterHipsterId
      uuid targetHipsterId
      bool isCorrect
      timestamp castAt
    }
```
> Note: **exhausted** applies per Coffee Bar. An Ingredient is scheduled once; if duplicates exist, they still count for correctness via associated **Submission** records.
---

## 10. API Design (REST)
Base URL: `/api/v1`

### 10.1 Coffee Bars
- `POST /coffeebars`
  - Body: `{ theme, submissionPolicy, defaultMaxPerHipster }`
  - Returns: `{ id, code, theme, submissionPolicy, defaultMaxPerHipster }`
- `GET /coffeebars/{code}`
  - Returns bar metadata and lobby summary.
- `POST /coffeebars/{code}/join`
  - Body: `{ username }`
  - Returns: `{ hipsterId, token }` (JWT for hub auth).
- `GET /coffeebars/{code}/leaderboard`
  - Returns cumulative scores per Hipster.
- `POST /coffeebars/{code}/close` (closes when exhausted; idempotent).

### 10.2 Ingredients & Submissions
- `POST /coffeebars/{code}/ingredients`
  - Body: `{ url or youtubeVideoId }`
  - Creates Ingredient (if new) and a **Submission** for caller.
- `GET /coffeebars/{code}/ingredients/summary`
  - Returns counts and your remaining quota.

### 10.3 Sessions & Rounds
- `POST /coffeebars/{code}/sessions`
  - Starts a new Brew Session.
- `POST /sessions/{sessionId}/next`
  - Picks next **unused Ingredient**, creates **BrewCycle**, broadcasts via Hub.
- `POST /cycles/{cycleId}/reveal`
  - Marks cycle as revealed; computes correctness; broadcasts results.
- `POST /cycles/{cycleId}/end`
  - Stops playback; persists final tallies.

### 10.4 Votes
- `POST /cycles/{cycleId}/votes`
  - Body: `{ targetHipsterId }`
  - Validates: one vote per voter; cannot vote for self; only while cycle active.

**Errors:** standard problem+json. Include `traceId` (W3C).

---

## 11. SignalR Hub Contract
**Hub:** `/hubs/brew`

### Server → Client
- `BarOpened(bar)` — initial bar state.
- `PresenceUpdated(listOfHipsters)` — who’s present now.
- `CycleStarted(cycleId, ingredient, playback)` — includes `videoId`, `startAt=0`.
- `PlaybackControl(action)` — `{ action: "play" | "pause" | "stop" }`.
- `VoteProgress(totalPresent, votesSoFar)` — `{ total, votedCount }`.
- `Reveal(cycleId, results)` — results include per-username vote counts, correctTargets list, correctGuessers list.
- `CycleEnded(cycleId)`
- `BarClosed()`

### Client → Server
- `Join(barCode, username)` → returns `hipsterId`.
- `Submit(url)` → returns `{ ingredientId, submissionId }`.
- `StartSession()` → returns `{ sessionId }`.
- `Next()` → starts next cycle.
- `Stop()` → broadcast stop.
- `CastVote(cycleId, targetHipsterId)` → returns `{ accepted: true }`.
- `Reveal(cycleId)` → trigger reveal (idempotent).

**Auth:** JWT bearer (scoped to bar + hipster).  
**Transport:** WebSockets preferred; allow fallback (Long Polling).

---

## 12. YouTube Sync Notes
- Use **IFrame API** (`YT.Player`) on each client; the hub broadcasts `play/pause/seek` intents.
- On `CycleStarted`, clients `cueVideoById(videoId)` then auto-`playVideo()`.
- Provide **“Resync”** button to `seekTo(serverTimestampOffset)` if desync detected (ads/skips).

---

## 13. Scoring & Leaderboards
- **Scoring**: voter gets **1 point** for each correct guess.
- **Tie-breakers**: none required; display shared ranks.
- **Leaderboards**:
  - **Per Bar**: total correct guesses.
  - **Global**: across all bars (sum; show top N).

---

## 14. Validation Rules
- **Username**: 3–20 chars; letters, digits, `_`, `-`; case-insensitive; unique per bar.
- **Room Code**: 6 chars; A–Z0–9; exclude vowels (`A,E,I,O,U`) to avoid accidental words.
- **Submissions**: valid YouTube video ID; normalize URLs; allow duplicates.
- **Voting**: one per cycle per present Hipster; cannot target own username.

---

## 15. Aspire App Model (Example)
```csharp
// AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("pg")
    .WithDataVolume()
    .WithPgAdmin() // optional
    .AddDatabase("coffeetalkdb");

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(postgres)
    .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
    .WithHttpEndpoint(env: "HTTP_PORT", port: 8080);

var frontend = builder.AddNpmApp("web", "../web")
    .WithHttpEndpoint(env: "PORT", port: 3000)
    .WithEnvironment("API_BASE_URL", "http://api:8080");

builder.AddProject<Projects.SignalR>("hub")
    .WithReference(postgres)
    .WithEnvironment("ASPNETCORE_URLS", "http://+:8090")
    .WithHttpEndpoint(env: "HUB_PORT", port: 8090);

builder.Build().Run();
```
> Note: PoC does **not** include Redis backplane. A single API/Hub app can be merged if simpler.

---

## 16. API & Persistence (Code-First Hints)
- Use **EF Core** with **NodaTime** or UTC DateTime.
- IDs are **GUID v7** (server-generated).
- Unique composite indexes:
  - `(CoffeeBar.Code)` unique.
  - `(Hipster.CoffeeBarId, UsernameLower)` unique.
  - `(Ingredient.CoffeeBarId, YouTubeVideoId)` unique for display-once semantics.
- Soft state (presence) kept in-memory per hub + heartbeat.

---

## 17. Testing Strategy
- **Unit**: domain services (submission normalization; vote validation; scoring).
- **Integration (minimal)**: API endpoints with in-memory test host and Postgres test container.
- **E2E (Playwright)**: join flow, submission, start round, vote, reveal chart.
- **CI (GitHub Actions)**: .NET 9 setup; Node 20; cache; run tests; upload Playwright trace on failure.

---

## 18. Telemetry
- **OTel** for API and Hub; W3C trace context on hub calls.
- Useful spans: `StartSession`, `NextCycle`, `CastVote`, `Reveal`.
- Metrics: cycles per session, avg votes per cycle, desync count (manual Resyncs).

---

## 19. Security Considerations (PoC)
- Treat room code as a secret; do not expose internal IDs in URLs.
- JWT contains `barCode` + `hipsterId`; short TTL with silent refresh over hub.
- No rate limits/profanity filters in PoC (documented risk).

---

## 20. Future Work (Beyond PoC)
- Designated Barista & moderator controls (kick/lock).
- Redis backplane for scale-out; Azure SignalR Service.
- OAuth login; profiles; avatars.
- Rate limiting & content/profanity filters.
- Rich analytics; per-round timers; auto-advance.
- Observer mode; chat; emoji reactions; skip vote.
- Per-video start timestamps; clipping.
- Exports; GDPR endpoints.

---

## 21. Minimal Domain Service Pseudocode
```csharp
// Selecting next unused Ingredient
Ingredient PickNextIngredient(Guid coffeeBarId) =>
    db.Ingredients.Where(i => i.CoffeeBarId == coffeeBarId && !i.Exhausted)
      .OrderBy(x => EF.Functions.Random())
      .FirstOrDefault();

// Compute correctness after votes
RevealResult ComputeReveal(Guid cycleId) {
  var cycle = db.BrewCycles.Include(x => x.Ingredient).First(c => c.Id == cycleId);
  var submitterIds = db.Submissions
      .Where(s => s.IngredientId == cycle.IngredientId)
      .Select(s => s.HipsterId).Distinct().ToHashSet();

  var votes = db.Votes.Where(v => v.BrewCycleId == cycleId).ToList();
  foreach (var v in votes) v.IsCorrect = submitterIds.Contains(v.TargetHipsterId);

  db.SaveChanges();
  var tally = votes.GroupBy(v => v.TargetHipsterId).ToDictionary(g => g.Key, g => g.Count());
  var correctGuessers = votes.Where(v => v.IsCorrect).Select(v => v.VoterHipsterId).ToList();
  return new RevealResult(tally, submitterIds, correctGuessers);
}
```

---

## 22. Sequence Diagrams
### 22.1 Join & Submit
```mermaid
sequenceDiagram
  participant Web as Frontend
  participant API as .NET API
  participant Hub as SignalR Hub
  participant DB as Postgres

  Web->>API: POST /coffeebars/{code}/join {username}
  API->>DB: Create/Find Hipster; issue JWT
  API-->>Web: { hipsterId, token }

  Web->>Hub: Join(barCode, username, token)
  Hub-->>Web: PresenceUpdated(...)

  Web->>API: POST /coffeebars/{code}/ingredients {url}
  API->>API: Normalize to videoId
  API->>DB: Upsert Ingredient; create Submission
  API-->>Web: { ingredientId, submissionId }
```

### 22.2 Brew Cycle & Reveal
```mermaid
sequenceDiagram
  participant Web
  participant Hub
  participant API
  participant DB

  Web->>API: POST /coffeebars/{code}/sessions
  API->>DB: Create BrewSession
  API-->>Web: { sessionId }

  Web->>API: POST /sessions/{sessionId}/next
  API->>DB: Pick unused Ingredient; create BrewCycle
  API-->>Hub: CycleStarted(cycleId, ingredient, playback)

  Web->>Hub: CastVote(cycleId, targetHipsterId)
  Hub->>DB: Persist Vote

  Web->>API: POST /cycles/{cycleId}/reveal
  API->>DB: Compute correctness; update scores
  API-->>Hub: Reveal(cycleId, results)
```
---

## 23. Environment & Config (PoC)
- `ASPNETCORE_ENVIRONMENT=Development`
- `ConnectionStrings__Postgres=...`
- `JWT__Issuer`, `JWT__Audience`, `JWT__Key`
- `YOUTUBE__ApiKey` (optional for metadata lookup; not required for playback)
- `FRONTEND__BaseUrl`, `API__BaseUrl`, `HUB__BaseUrl`

---

## 24. Repository Hygiene
- License: **MIT**
- `CONTRIBUTING.md` & `CODE_OF_CONDUCT.md`
- Top-level `README.md` with quickstart & Aspire instructions.
- Scripts:
  - `./scripts/dev-seed.ps1` (optional; PoC picked **No seeding** for now)
  - `./scripts/run-e2e.ps1` (Playwright)

---

## 25. Acceptance Criteria (PoC)
1. Create Coffee Bar with theme/topic, default max per Hipster, and submission policy.
2. Join with code + username; username unique within bar.
3. Submit YouTube links; duplicates allowed; normalize to video ID.
4. Start session; clients see synchronized playback.
5. Cast votes; cannot vote for yourself.
6. Reveal shows bar chart with ✓ on correct submitter(s) and sidebar of correct guessers.
7. Manual **Next** advances; after all Ingredients are used across sessions, Coffee Bar closes.
8. Per-bar and global leaderboards update in real time.
9. CI runs unit, minimal integration, and Playwright tests; OTel telemetry visible via Aspire.

---

**End of PRD**  
