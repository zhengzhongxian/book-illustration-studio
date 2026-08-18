# Architecture Overview & Specifications

## 1. System Pipeline Architecture (5-Step Sequential Workflow)

```mermaid
flowchart TD
    A[Book Text Upload / Paste] --> S1[Step 1: Style Definition]
    S1 -->|Custom or Generated Style| S2[Step 2: Adult Characters Extraction]
    S2 -->|Max 2 Characters JSON Schema| S3[Step 3: Character Portraits Generation]
    S3 -->|9:16 PNG Local Storage| S4[Step 4: Chapter Prompts Extraction]
    S4 -->|Max 1 Chapter JSON Schema| S5[Step 5: Chapter Illustration Generation]
    S5 -->|16:10 PNG with Multimodal Ref| D[Project Complete: DONE]

    classDef step fill:#FFEEDF,stroke:#FF6B00,stroke-width:2px,color:#231F20;
    classDef done fill:#231F20,stroke:#231F20,stroke-width:2px,color:#FFFFFF;
    class S1,S2,S3,S4,S5 step;
    class D done;
```

---

## 2. Dual-Status State Machine

To guarantee that mid-step refreshes, page reloads, and network interruptions are handled without losing data or causing stuck states, state is tracked with **two independent flat fields** on each `Project` row:

- **`Status`** (Milestone): Records the *last completed* pipeline step. Only advances forward on success.
- **`StepState`** (Runtime): Records whether any step is *currently executing*. Resets on completion or failure.

### Milestone Progression (`Project.Status`)

```mermaid
stateDiagram-v2
    [*] --> CREATED : Project created
    CREATED --> STYLE_SET : Step 1 succeeds
    STYLE_SET --> CHARACTERS_GENERATED : Step 2 succeeds (≤2 chars)
    CHARACTERS_GENERATED --> PORTRAITS_GENERATED : Step 3 succeeds
    PORTRAITS_GENERATED --> CHAPTERS_GENERATED : Step 4 succeeds (≤1 chapter)
    CHAPTERS_GENERATED --> DONE : Step 5 succeeds
```

### Runtime Execution Cycle (`Project.StepState`)

Every step follows the same cycle — `StepState` is shared across all steps:

```mermaid
stateDiagram-v2
    [*] --> IDLE
    IDLE --> RUNNING : User triggers next step
    RUNNING --> IDLE : Step succeeds (Status advances)
    RUNNING --> FAILED : API / network error (Status unchanged)
    FAILED --> RUNNING : User retries same step
    RUNNING --> IDLE : Reset stuck step (manual recovery)

    note right of RUNNING : StepStartedAt = UTC now\nLastError = null
    note right of FAILED : LastError = error message\nStepStartedAt preserved
    note right of IDLE : StepStartedAt = null
```

### How a Browser Refresh Reads State

| `Status` | `StepState` | UI Interpretation |
|:---|:---|:---|
| `CHARACTERS_GENERATED` | `IDLE` | Step 2 done. Show "Generate Portraits" button. |
| `CHARACTERS_GENERATED` | `RUNNING` | Step 3 in-flight. Show spinner + poll for portraits. |
| `CHARACTERS_GENERATED` | `FAILED` | Step 3 failed. Show error + Retry button. |
| `CHARACTERS_GENERATED` | `RUNNING` + stale `StepStartedAt` | Step 3 stuck (server died). Show recovery affordance. |


---

## 3. Concurrency & Locking Model

```mermaid
sequenceDiagram
    autonumber
    actor User as Client (Tab 1 / Double-click)
    actor User2 as Client (Tab 2 / Refresh)
    participant API as Studio.Api (PipelineController)
    participant Guard as ConcurrencyGuard (SemaphoreSlim)
    participant DB as SQLite DB (WAL Mode)
    participant Gemini as Google Gemini REST API

    User->>API: POST /api/projects/{id}/steps/CHARACTERS
    API->>Guard: AcquireLockAsync(projectId)
    Guard-->>API: Lock Granted
    API->>DB: Update StepState = RUNNING, StepStartedAt = Now
    
    par In-Flight Execution
        User2->>API: POST /api/projects/{id}/steps/CHARACTERS
        API->>Guard: TryAcquire(projectId)
        Guard-->>API: Lock Busy / Already Running
        API-->>User2: 409 Conflict (Step is already running)
    and
        API->>Gemini: POST generateContent (Structured Schema)
        Gemini-->>API: Return JSON Characters (Cap <= 2)
        API->>DB: Save Characters, Status = CHARACTERS_GENERATED, StepState = IDLE
        API->>Guard: ReleaseLock(projectId)
        API-->>User: 200 OK (Project Updated)
    end
```

---

## 4. Entity Relationship Schema

```mermaid
erDiagram
    USER ||--o{ PROJECT : owns
    PROJECT ||--o{ CHARACTER : contains
    PROJECT ||--o{ CHAPTER : contains

    USER {
        string Id PK
        string Email UK
        string Name
        datetime CreatedAt
    }

    PROJECT {
        string Id PK
        string UserId FK
        string Title
        string BookText
        string Status
        string StepState
        string LastError
        datetime StepStartedAt
        string Style
        datetime CreatedAt
    }

    CHARACTER {
        string Id PK
        string ProjectId FK
        string Name
        string Prompt
        string PortraitPath
        boolean PortraitReady
        int SortOrder
    }

    CHAPTER {
        string Id PK
        string ProjectId FK
        string Name
        string Prompt
        string CharactersJson
        string IllustrationPath
        boolean IllustrationReady
        int SortOrder
    }
```
