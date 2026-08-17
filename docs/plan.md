# Detailed Execution Plan — Book Illustration Studio

> **Project:** Book Illustration Studio (Gradion Take-Home Assessment)  
> **Duration:** 3 Focused Shifts (~16 Hours equivalent)  
> **Target:** Robust, resumable, concurrent-safe full-stack web app illustrating books with Gemini REST API.

---

## Shift 1 · Architecture, Core Backend & Gemini REST Integration
**Focus:** Establish robust data modeling, concurrency guards, and direct REST communication with Google Gemini.

### Milestones
1. **Repository & Context Setup**:
   - Initialize `.gitignore`, `.antigravity/` custom skills, and `AGENTS.md`.
   - Setup project architecture blueprint in `docs/architecture.md`.
2. **Backend Domain & Data Layer**:
   - Create .NET 8 Web API solution (`Studio.sln`, `Studio.Api.csproj`).
   - Define Entities: `User`, `Project`, `Character`, `Chapter`.
   - Setup `StudioDbContext` (SQLite with WAL mode, foreign keys, timestamps).
3. **Gemini REST Client**:
   - Build strongly-typed `GeminiRestClient` using native `HttpClient`.
   - Implement Structured JSON generation for Style, Characters (Schema: `name`, `prompt`), Chapters (`name`, `prompt`, `characters`).
   - Implement Image Generation (Aspect Ratio: 9:16 for portraits, 16:10 for chapter illustration) with multimodal reference images.
4. **Pipeline Orchestrator & Concurrency Guard**:
   - Implement `PipelineService` with in-memory `ConcurrentDictionary<string, SemaphoreSlim>` per-project locking.
   - Enforce hard limits (max 2 characters, max 1 chapter).
   - Implement `reset-stuck` recovery endpoint.

---

## Shift 2 · Frontend Implementation & Gradion Design System
**Focus:** Build pixel-perfect React + TypeScript UI adhering to `app-demo.html` tokens and state transitions.

### Milestones
1. **Frontend Scaffolding**:
   - Initialize Vite + React + TypeScript in `frontend/`.
   - Port design tokens from `app-demo.html` (`tokens.css`, `components.css`, `animations.css`).
2. **State Management & API Client**:
   - Create typed API client (`services/api.ts`) for auth, projects, pipeline execution, and image serving.
   - Implement polling mechanism for running steps with graceful error recovery.
3. **UI Components & Pages**:
   - `AuthPage`: Email + Name sign-in (lightweight session).
   - `ProjectListPage`: Project rows, progress mini-segments, status pills (`Draft`, `In progress`, `Done`).
   - `NewProjectPage`: `.txt` drag-and-drop dropzone + text area.
   - `ProjectDetailPage`: Dynamic 5-step Stepper with pulsing current step, Entity cards (aspect ratios 3:4 & 16:10), action panel, and accessible full-book text modal.
4. **Edge Cases & Error Handling**:
   - Specific in-progress state messaging.
   - Retry button for failed steps.
   - Interrupted/stuck step recovery banner.

---

## Shift 3 · Automated Testing, Documentation & Final Polish
**Focus:** Comprehensive test coverage, real test execution report, and submission deliverables.

### Milestones
1. **Backend Tests (`Studio.Tests`)**:
   - `PipelineStateMachineTests`: State transition verification, step ordering.
   - `ConcurrencyTests`: Prevention of duplicate calls on concurrent/multi-tab requests.
   - `CapEnforcementTests`: Server-side rejection if exceeding 2 characters or 1 chapter.
   - `StuckStepRecoveryTests`: Validating recovery flow.
2. **Frontend Tests**:
   - Vitest component tests for Stepper, EntityCard, and Error states.
3. **Documentation & Deliverables**:
   - `DECISIONS.md`: 4–6 technical trade-offs + 3 AI override explanations + roadmap.
   - `TESTING.md`: Test strategy + actual raw test execution outputs.
   - `README.md`: 1-line start and 1-line test instructions.
   - Startup scripts: `start.sh`, `start.bat`, `test.sh`, `test.bat`.
