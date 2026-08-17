---
name: state-machine-concurrency
description: Rules and patterns for pipeline state transitions, per-project locking, idempotent execution, and stuck-step recovery.
---

# State Machine and Concurrency Guide

## State Architecture

The application splits project lifecycle into two distinct properties:
1. `Status`: Indicates overall completed milestone:
   - `CREATED` (Step 0)
   - `STYLE_SET` (Step 1 complete)
   - `CHARACTERS_GENERATED` (Step 2 complete)
   - `PORTRAITS_GENERATED` (Step 3 complete)
   - `CHAPTERS_GENERATED` (Step 4 complete)
   - `DONE` (Step 5 complete)

2. `StepState`: Indicates runtime execution status:
   - `IDLE`: No task currently in flight. Safe to start next step.
   - `RUNNING`: A background Gemini request is currently executing.
   - `FAILED`: Previous step run failed with an error. Contains `LastError` message.

## Concurrency Guard (Per-Project Locking)

- Multi-tab navigation, page refresh, or rapid double-clicks must **never** duplicate Gemini API requests.
- Backend uses an in-memory `ConcurrentDictionary<string, SemaphoreSlim>` to acquire exclusive per-project execution lock.
- If a request arrives while `StepState == RUNNING`, the API immediately returns `409 Conflict` or returns the current active in-flight state without firing a duplicate Gemini API call.

## Resumability & Stuck Step Recovery

- All step results (style, character entities, prompts, local image file paths) are committed to SQLite atomically upon completion.
- If the server or process terminates unexpectedly while `StepState == RUNNING`:
  - The client detects that `StepStartedAt` exceeds timeout threshold or presents a `Retry` / `Reset Stuck Step` affordance.
  - Endpoint `POST /api/projects/{id}/reset-stuck` clears `StepState` to `IDLE` without mutating previously completed milestone results.
