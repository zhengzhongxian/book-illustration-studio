# TESTING.md — Testing Strategy & Verification Report

## 1. Testing Strategy & Philosophy

Our testing strategy follows the assessment's guidelines (§5.4): focus on critical logic boundaries, state transitions, concurrency safety, and resilient UI states rather than superficial code coverage percentages.

### 1.1 Backend Testing (`backend/tests/Studio.Tests/`)
- **State Machine & Pipeline Sequence (`PipelineStateMachineTests.cs`)**:
  - Validates that Step 2 cannot execute before Step 1, Step 3 cannot execute before Step 2, and so forth.
  - Verifies complete happy-path milestone progressions (`CREATED` -> `STYLE_SET` -> `CHARACTERS_GENERATED` -> `PORTRAITS_GENERATED` -> `CHAPTERS_GENERATED` -> `DONE`).
- **Hard Cap Enforcements (`CapEnforcementTests.cs`)**:
  - Enforces server-side limits: even if the LLM produces $>2$ characters or $>1$ chapter, the system strictly truncates to max 2 adult characters and max 1 chapter illustration.
- **Concurrency & Duplicate Guard (`ConcurrencyAndLockTests.cs`)**:
  - Validates that two simultaneous requests on the same project reject the second request with a concurrency conflict exception while the first is in-flight.
- **Stuck-Step Recovery (`StuckStepRecoveryTests.cs`)**:
  - Ensures stalled steps can transition back to `IDLE` cleanly without corrupting previously generated assets.

### 1.2 Frontend Testing (`frontend/src/__tests__/`)
- **`Stepper.test.tsx`**: Verifies all 5 milestone labels render, active step exhibits pulsing style, and completed milestones render checkmarks.
- **`EntityCard.test.tsx`**: Tests pending placeholder, live generation spinner, and rendered image states.
- **`ProjectRow.test.tsx`**: Tests mini-progress segment calculations and status pills (`Draft`, `In progress`, `Done`).

### 1.3 What We Deliberately Did Not Test & Why
- **Live Gemini API in Automated Tests**: We mocked `IGeminiClient` to avoid burning API quotas, rate-limit thrashing, and non-deterministic network failures in automated CI runs.
- **End-to-End Browser Automation**: Heavy browser orchestration (Playwright/Cypress) adds brittle timing overhead for an application whose core invariants are already protected by unit and integration tests.

---

## 2. Actual Test Execution Logs

Below are the verbatim execution reports from running the test suite on August 17, 2026.

### 2.1 Backend xUnit Test Run
```text
$ dotnet test backend/Studio.slnx
  Determining projects to restore...
  All projects are up-to-date for restore.
  Studio.Api -> D:\Apply\book-illustration-studio\backend\src\Studio.Api\bin\Debug\net8.0\Studio.Api.dll
  Studio.Tests -> D:\Apply\book-illustration-studio\backend\tests\Studio.Tests\bin\Debug\net8.0\Studio.Tests.dll
Test run for D:\Apply\book-illustration-studio\backend\tests\Studio.Tests\bin\Debug\net8.0\Studio.Tests.dll (.NETCoreApp,Version=v8.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:     7, Skipped:     0, Total:     7, Duration: 1 s - Studio.Tests.dll (net8.0)
```

### 2.2 Frontend Vitest Test Run
```text
$ npm --prefix frontend test

> book-illustration-studio-frontend@1.0.0 test
> vitest run

 RUN  v4.1.10 D:/Apply/book-illustration-studio/frontend

 ✓ src/__tests__/Stepper.test.tsx (3 tests) 68ms
 ✓ src/__tests__/ProjectRow.test.tsx (2 tests) 95ms
 ✓ src/__tests__/EntityCard.test.tsx (3 tests) 202ms

 Test Files  3 passed (3)
      Tests  8 passed (8)
   Start at  23:02:33
   Duration  3.38s (transform 159ms, setup 714ms, import 317ms, tests 366ms, environment 5.21s)
```
