# AGENTS.md — Book Illustration Studio Guidelines

## 1. Project Mission & Identity
Book Illustration Studio is a full-stack application built to illustrate books using Google Gemini REST APIs. It guides users through an ordered 5-step pipeline:
1. **Style**: Define or generate art style.
2. **Characters**: Extract up to 2 adult characters with visual prompts.
3. **Portraits**: Generate portrait images (9:16) for each character.
4. **Chapters**: Extract up to 1 chapter scene prompt referencing the characters.
5. **Illustrations**: Generate a chapter illustration (16:10) referencing the character portraits.

---

## 2. Core Architecture & Tech Stack
- **Backend**: .NET 8 / 10 Web API (`backend/src/Studio.Api/`)
  - Storage: EF Core with SQLite in WAL mode.
  - LLM & Image Client: Direct REST calls via `HttpClient` (Gemini 2.5/1.5 & Flash Image / Imagen).
  - Concurrency: `SemaphoreSlim` per-project in-memory locking.
- **Frontend**: React + TypeScript + Vite (`frontend/`)
  - Styling: Pure CSS design tokens strictly matching the Gradion Design System from `app-demo.html`.
  - State: React hooks, resilient error banners, retry buttons, and stuck-step recovery affordances.
- **Tests**:
  - Backend: xUnit (`backend/tests/Studio.Tests/`)
  - Frontend: Vitest (`frontend/src/__tests__/`)

---

## 3. Strict Rules & Engineering Standards

### 3.1 Pipeline Boundaries & Constraints
- **Character Cap**: Hard limit of maximum **2 adult characters**. Never exceed 2.
- **Chapter Cap**: Hard limit of maximum **1 chapter illustration**. Never exceed 1.
- **Sequential Execution**: Steps must be run in exact order (1 -> 2 -> 3 -> 4 -> 5).
- **Cost Discipline**: Reuse context; never re-upload full book text unnecessarily. Retries are user-triggered only.

### 3.2 State Modeling
- `Status`: Represents completed milestone (`CREATED`, `STYLE_SET`, `CHARACTERS_GENERATED`, `PORTRAITS_GENERATED`, `CHAPTERS_GENERATED`, `DONE`).
- `StepState`: Represents runtime execution (`IDLE`, `RUNNING`, `FAILED`).
- `LastError`: Human-readable error message populated on failure.
- `StepStartedAt`: Timestamp used to detect stalled/interrupted executions.

### 3.3 Git Commit Conventions
All commit messages must follow the project standard:
- Prefix `+ ` for adding a new feature or file.
- Prefix `- ` for removing or deleting code/files.
- Prefix `* ` for refactoring or fixing existing logic.
- Title Case in English (e.g. `+ Add User Authentication And Project Persistence.`).
- Push after every feature commit (`git push origin main`).

### 3.4 AI Copilot Discipline
- Never write speculative, unused abstractions (e.g., custom generic `UnitOfWork` wrappers over EF Core).
- All AI overrides, design trade-offs, and critical decisions must be documented in `DECISIONS.md`.
