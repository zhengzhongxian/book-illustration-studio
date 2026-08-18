# Book Illustration Studio

> A full-stack web application that illustrates books using Google Gemini REST APIs, built with .NET 8 Web API, SQLite, and React + TypeScript + Vite adhering to the Gradion Design System.

---

## 1. Quick Start (1-Command Startup)

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (v18+ recommended)
- A Google Gemini API Key (optional for UI review, required for live LLM & image generation)

### Configuration
Copy `.env.example` to `.env` or configure your key in `backend/src/Studio.Api/appsettings.json`:
```bash
cp .env.example .env
# Set your key in .env: Gemini__ApiKey=your_actual_key_here
```


> [!NOTE]
> **Important Note for Reviewers Regarding Gemini API Keys & Rate Limits**:
> - **Enterprise / Billing-Enabled Keys**: When provided with a key linked to a paid/active billing account, the system calls Google's live endpoints and generates real AI PNG portraits and chapter scene illustrations.
> - **Free-Tier / Personal Keys**: Google AI Studio enforces strict free-tier quotas (`limit: 0` for image generation models or prepayment restrictions). When a 429 `RESOURCE_EXHAUSTED` / `Prepay credits depleted` response is returned by Google, our backend captures the error gracefully and provides an inline error message with a **Retry** button and resilient fallback storybook illustrations, ensuring the application never crashes.
> - **Zero-Quota Test Suite**: Reviewers can run `./test.sh` or `test.bat` to verify 100% of the core business logic (15/15 unit and integration tests) completely offline without consuming any API quota or tokens thanks to mocked interfaces.

### Start Both Backend & Frontend in 1 Command
- **macOS / Linux**:
  ```bash
  ./start.sh
  ```
- **Windows**:
  ```cmd
  start.bat
  ```

- Frontend: [http://localhost:5173](http://localhost:5173)
- Backend API: [http://localhost:5000](http://localhost:5000)

---

## 2. Run All Tests (1-Command Verification)

Runs both backend xUnit tests and frontend Vitest test suites:

- **macOS / Linux**:
  ```bash
  ./test.sh
  ```
- **Windows**:
  ```cmd
  test.bat
  ```

---

## 3. Architecture & Pipeline Overview

The application follows the 5-step pipeline specified in the Google Gemini Book Illustration cookbook:

```
[Book Text] 
   │
   ├─► Step 1: Style ─────────► Define or auto-generate art style
   ├─► Step 2: Characters ────► Extract max 2 adult character visual prompts (JSON Schema)
   ├─► Step 3: Portraits ─────► Generate 9:16 portrait images for each character
   ├─► Step 4: Chapters ──────► Extract max 1 chapter scene prompt referencing characters
   └─► Step 5: Illustrations ─► Generate 16:10 scene illustration using portraits as multimodal references
```

### Key Technical Attributes
- **Dual-Status State Machine**: `Status` tracks completed milestones (`CREATED` to `DONE`), while `StepState` (`IDLE`, `RUNNING`, `FAILED`) tracks active execution.
- **Concurrency & Duplicate Protection**: In-memory `SemaphoreSlim` per-project lock prevents duplicate API calls across multi-tabs, page refreshes, and rapid clicks.
- **Stuck-Step Recovery**: Stalled steps can be reset to `IDLE` with one click without losing previously generated assets.
- **Strict Server-Side Caps**: Bounded at **maximum 2 characters** and **maximum 1 chapter** to strictly control API usage.
- **Storage**: In-process EF Core SQLite with Write-Ahead Logging (WAL) mode; generated PNG images are served directly from disk storage (`/api/images/...`).

---

## 4. Documentation Index

- [DECISIONS.md](file:///d:/Apply/book-illustration-studio/DECISIONS.md) — 6 core technical decisions, trade-offs, 4 AI pushback callouts, and next-day roadmap.
- [TESTING.md](file:///d:/Apply/book-illustration-studio/TESTING.md) — Testing strategy and actual raw test execution reports.
- [AGENTS.md](file:///d:/Apply/book-illustration-studio/AGENTS.md) — AI Copilot guidelines and commit conventions.
- [docs/plan.md](file:///d:/Apply/book-illustration-studio/docs/plan.md) — 3-shift project implementation plan.
- [docs/architecture.md](file:///d:/Apply/book-illustration-studio/docs/architecture.md) — Mermaid pipeline, state machine, and concurrency diagrams.
- [docs/ai-prompts.md](file:///d:/Apply/book-illustration-studio/docs/ai-prompts.md) — Prompt logs, agent configuration, and custom skills documentation.

