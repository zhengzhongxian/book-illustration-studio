# AI Prompts & Agent Configuration Log

> **Tool:** Google Antigravity IDE (Gemini-based agentic coding assistant)
> **Project Context File:** `AGENTS.md` (loaded automatically on every session)
> **Custom Skills:** `.antigravity/skills/` (3 workflow skills created during development)

---

## Key Prompts Used During Development

### Phase 1 — Architecture & Backend Foundation

**Prompt: Initial Pipeline Design**
```
Build a .NET 8 Web API for a book illustration pipeline with 5 sequential steps:
Style → Characters → Portraits → Chapters → Illustrations.
Use EF Core with SQLite in WAL mode. Enforce max 2 adult characters and max 1 chapter
server-side. Each step must be user-triggered, resumable, and protected against
duplicate execution via in-memory SemaphoreSlim per-project locking.
```

**Prompt: Gemini REST Client (no SDK)**
```
Implement a GeminiRestClient using native HttpClient — no third-party SDK.
Map each notebook step to its REST endpoint:
- Text generation: POST /v1beta/models/{model}:generateContent with JSON schema
- Image generation: POST /v1beta/models/{model}:generateContent with responseModalities: ["TEXT", "IMAGE"]
- Multimodal context: inline Base64 image parts for character portrait references in Step 5.
Support structured JSON output with responseSchema for characters and chapters extraction.
```

**Prompt: Dual-State Machine Design**
```
I rejected your single status enum. Split state into two fields:
- Status (milestone): CREATED → STYLE_SET → CHARACTERS_GENERATED → PORTRAITS_GENERATED → CHAPTERS_GENERATED → DONE
- StepState (runtime): IDLE → RUNNING → FAILED
This lets a browser refresh mid-step read the correct state: "Step 3 completed, Step 4 in-flight."
Add StepStartedAt timestamp for stuck-step detection and LastError for failure messages.
```

---

### Phase 2 — Frontend & Design System

**Prompt: Design System Extraction**
```
Extract all CSS design tokens from app-demo.html — colors, typography, spacing,
border-radius, shadows, and animations. Port them into pure CSS custom properties
in tokens.css. The frontend must visually match or beat app-demo.html.
No TailwindCSS. No component libraries. Pure CSS with design tokens.
```

**Prompt: Polling & Progressive Updates**
```
Implement short polling (2.5s interval) while stepState === 'RUNNING'.
For Step 3 (portraits), poll and render each portrait as it completes —
the user sees each portrait land individually, not one long blocking wait.
Stop polling when stepState transitions to IDLE or FAILED.
```

---

### Phase 3 — Testing & Hardening

**Prompt: Backend Test Suite**
```
Write xUnit tests for PipelineService covering:
1. Step ordering enforcement (cannot skip steps)
2. Concurrent execution rejection (409 Conflict)
3. Hard cap enforcement (max 2 characters, max 1 chapter)
4. Stuck-step recovery (reset stale RUNNING states)
All tests must use EF Core InMemory provider — zero Gemini API calls.
Mock IGeminiClient to return deterministic structured data.
```

**Prompt: Frontend Test Suite**
```
Write Vitest tests for React components:
- Stepper component state rendering (done/current/pending)
- Error banner display and retry button interaction
- Entity cards rendering with and without images
- Loading and empty states
```

---

### Phase 4 — Configuration Refactoring (AI Override)

**Prompt: Rejecting Verbose Config Parsers**
```
Your manual foreach loops parsing .env with string splits and substring matching
are reinventing what .NET already does natively. Remove all of it.

Use DotNetEnv.Env.TraversePath().Load() before CreateBuilder.
Use standard Gemini__ double-underscore naming in .env.
Let WebApplication.CreateBuilder bind environment variables to GeminiOptions
automatically via the Options Pattern. Zero manual boilerplate.
```

---

## Custom Agent Skills Created

| Skill | Path | Purpose |
|:---|:---|:---|
| `gemini-pipeline-workflow` | `.antigravity/skills/gemini-pipeline-workflow/` | Step-by-step pipeline execution patterns and Gemini REST call chaining |
| `gradion-design-system` | `.antigravity/skills/gradion-design-system/` | CSS design tokens and component styling rules from `app-demo.html` |
| `state-machine-concurrency` | `.antigravity/skills/state-machine-concurrency/` | Dual-state machine transitions and `SemaphoreSlim` concurrency patterns |

---

## Agent Configuration

**`AGENTS.md`** served as the persistent project context file, loaded at the start of every AI session. It defined:
- Pipeline boundaries (2 character cap, 1 chapter cap, sequential execution)
- State modeling rules (Status vs StepState vs LastError vs StepStartedAt)
- Git commit conventions (prefix `+ `, `- `, `* ` with Title Case)
- Engineering standards (no speculative abstractions, all decisions documented)
- Two-tier resilience pattern (live Gemini → graceful degradation)

**Session continuity** was maintained through Antigravity's context management — the agent carried forward knowledge of completed milestones, file states, and design decisions across multiple coding sessions.
