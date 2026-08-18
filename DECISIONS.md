# DECISIONS.md — Technical Decisions & AI Copilot Evaluation

## 1. Core Engineering Decisions & Trade-Offs

### Decision 1: SQLite with WAL Mode over JSON Flat Files or External Database
- **Context & Proposal**: AI initially proposed storing everything in flat JSON files on disk per project to avoid database configuration.
- **Pushback & Resolution**: I pushed back. While JSON files satisfy zero-config requirements, managing concurrent step updates, transactional atomicity, cascade deletions, and index searches across projects becomes messy and error-prone. We selected EF Core with SQLite in Write-Ahead Logging (WAL) mode (`PRAGMA journal_mode=WAL;`).
- **Cost & Trade-Off**: Requires EF Core dependencies, but gives full ACID transaction guarantees, fast in-process query execution, and zero external infrastructure setup.

### Decision 2: Split Status (`ProjectStatus`) and Runtime Execution State (`StepState`)
- **Context & Proposal**: AI proposed a single combined status enum (e.g. `STYLE_RUNNING`, `STYLE_DONE`, `CHARACTERS_RUNNING`).
- **Pushback & Resolution**: I pushed back — a single enum cannot cleanly express "Step 3 completed, Step 4 currently in-flight" during a browser refresh. We split the state into `Status` (milestone tracker: `CREATED`, `STYLE_SET`, `CHARACTERS_GENERATED`, `PORTRAITS_GENERATED`, `CHAPTERS_GENERATED`, `DONE`) and `StepState` (`IDLE`, `RUNNING`, `FAILED`).
- **Cost & Trade-Off**: Two state fields to coordinate in database updates, and orphaned `RUNNING` states require explicit timeout detection and reset affordances.

### Decision 3: Direct Native REST Client over Third-Party Google SDK
- **Context & Proposal**: AI proposed searching for an unofficial community C# SDK or using Python sidecars.
- **Pushback & Resolution**: I overrode this. The Google Gemini documentation confirms that all structured JSON outputs, interactions, and image generation endpoints are plain HTTP REST endpoints (`POST /v1beta/models/...:generateContent`). Using .NET's native `HttpClient` with typed DTOs and `System.Text.Json` gives 100% control, zero external SDK version churn, and exact alignment with the cookbook notebook.
- **Cost & Trade-Off**: We manually defined the request/response JSON schemas, but gained complete transparency and zero third-party dependency vulnerabilities.

### Decision 4: In-Memory `SemaphoreSlim` Per-Project Locking for Concurrency
- **Context & Proposal**: AI suggested storing lock timestamps directly in the database row with optimistic concurrency checks.
- **Pushback & Resolution**: I pushed back because rapid double-clicks and multi-tab browser refreshes within milliseconds can slip past optimistic DB checks if connection pooling lags. We implemented an in-memory `ConcurrentDictionary<string, SemaphoreSlim>` guard that provides non-blocking lock acquisition (`semaphore.Wait(0)`), immediately rejecting concurrent duplicate attempts with `409 Conflict`.
- **Cost & Trade-Off**: In-memory locks apply to a single server instance (scaling to multi-instance would require Redis distributed locks), but perfectly matches local and single-node deployment.

### Decision 5: Declining Redundant `UnitOfWork` & Generic Repository Wrappers
- **Context & Proposal**: AI boilerplate scaffolding attempted to introduce `IUnitOfWork`, `IRepository<T>`, and multiple wrapper classes over EF Core.
- **Pushback & Resolution**: I rejected this abstraction per §05 ("Keep it simple and lean. Do not over-engineer"). In EF Core, `DbContext` is already a Unit of Work and `DbSet<T>` is a Repository. Wrapping them adds artificial complexity, hides async LINQ capabilities, and offers zero testability benefit over EF Core's built-in In-Memory test provider.
- **Cost & Trade-Off**: Services call `_db` directly, so switching to a different ORM later would touch every service. Acceptable at this scope — the wrappers would cost more to maintain than they save.

### Decision 6: Two-Tier Architecture for Live Gemini API & Graceful Degradation
- **Context & Proposal**: AI initially proposed crashing or bubbling up raw HTTP 429 exceptions to the frontend whenever Google AI Studio rate-limits or returns `RESOURCE_EXHAUSTED` / `Prepay credits depleted`.
- **Pushback & Resolution**: I pushed back. In a real-world production system, third-party API rate limits and regional billing tier quirks are inevitable. We architected a resilient two-tier design:
  1. *Live Tier*: Calls Google's official endpoints directly with current model candidates (`gemini-2.5-flash`, `gemini-2.0-flash`, `gemini-1.5-flash`). If provided with a billing-enabled key, live AI outputs are generated.
  2. *Graceful Degradation Tier*: When Google returns HTTP 429 / 503 (free-tier quota exhaustion), the backend captures the error, logs it, returns an SVG fallback illustration, and surfaces an inline error state with a **Retry** button — without corrupting completed milestones.
- **Cost & Trade-Off**: Requires maintaining fallback SVG generation routines alongside the live path, and reviewers using free-tier keys will only see placeholder art until they retry with quota available.

---

## 2. Where I Overrode the AI (Copilot Pushback Callouts)

### Override 1: Rejecting Client-Side Duplicate Call Prevention
- **What AI did wrong**: AI placed the duplicate-click guard in the frontend React state (`isSubmitting` flag in local component state).
- **Why it was unsafe**: Client-side guards do not protect against page refreshes, requests from a second browser tab, or network latency spikes.
- **What I did instead**: Built a server-side concurrency guard (`ProjectLockService`) backed by `SemaphoreSlim` and atomic database `StepState` tracking. If another call arrives while a step is running, the server rejects it with a `409 Conflict` and returns the existing in-flight state.
- **Cost I accepted**: In-memory locks are single-instance only and add a `ConcurrentDictionary` that grows with project count without eviction.

### Override 2: Correcting Hard Pipeline Caps Server-Side
- **What AI did wrong**: AI wrote prompt text asking Gemini for 2 characters and 1 chapter, but did not enforce the caps in code, assuming Gemini would always obey.
- **Why it was unsafe**: LLMs frequently return extra entities (e.g., 4 characters or 3 chapters) despite prompt instructions, which would violate the assessment's cost constraints.
- **What I did instead**: Added explicit LINQ `.Take(2)` and `.Take(1)` enforcement in `PipelineService.cs` and `GeminiRestClient.cs` to guarantee that database persistence and subsequent image generation loops never exceed 2 character portraits and 1 chapter illustration.
- **Cost I accepted**: If the assessment caps change, the hard-coded `.Take(N)` values must be updated in two places — but a magic number beats a silent budget overrun.

### Override 3: Fixing Multimodal Image Context Chaining in Step 5
- **What AI did wrong**: AI attempted to describe characters solely with text in Step 5 without passing the portrait image files.
- **Why it was wrong**: The notebook specifically demonstrates using generated character portraits as multimodal visual references to maintain character visual consistency across scenes.
- **What I did instead**: Implemented reading the local portrait PNG bytes, encoding them to Base64 `inlineData` parts, and attaching them alongside the chapter prompt in the Gemini REST payload.
- **Cost I accepted**: Base64 encoding doubles the payload size per portrait (~200 KB each), increasing request latency by a few hundred milliseconds.

### Override 4: Enforcing Native .NET Options Pattern over Manual Config Parsers
- **What AI did wrong**: AI wrote verbose file-reading loops, string splits, manual substring manipulations, and multiple imperative `PostConfigure` calls in `Program.cs` to map `.env` variables into `GeminiOptions`.
- **Why it was overcomplicated**: It reinvented the wheel, cluttered application bootstrapping, and was brittle when introducing new configuration properties.
- **What I did instead**: I pushed back on the AI and instructed it to adopt the official .NET Options Pattern standard with `DotNetEnv` and hierarchical double-underscore naming (`Gemini__ApiKey`, `Gemini__TextModel`). By calling `DotNetEnv.Env.TraversePath().Load()`, `WebApplication.CreateBuilder(args)` natively binds environment variables into `GeminiOptions` automatically with zero manual boilerplate.

---

## 3. If I Had One More Day: What I Would Build Next & Why

If granted one additional day, I would build **Server-Sent Events (SSE) / WebSocket live streaming for image generation progress**:
- **Why**: Currently, the frontend uses short polling (`2.5s` intervals) to check if an in-flight image generation has completed. While robust and resilient to disconnects, an SSE stream would push individual portrait completion events in real-time as each image is written to disk, eliminating polling overhead and providing an instantaneous visual reveal.
