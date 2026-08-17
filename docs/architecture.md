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

To guarantee that mid-step refreshes, page reloads, and network interruptions are handled without losing data or causing stuck states, state is separated into **Milestone Status** and **Runtime Step State**.

```mermaid
stateDiagram-v2
    [*] --> CREATED

    state CREATED {
        [*] --> IDLE_0
        IDLE_0 --> RUNNING_1: Start Step 1 (Style)
        RUNNING_1 --> FAILED_1: API / Network Error
        FAILED_1 --> RUNNING_1: User Retry Step 1
        RUNNING_1 --> STYLE_SET: Success
    }

    state STYLE_SET {
        [*] --> IDLE_1
        IDLE_1 --> RUNNING_2: Start Step 2 (Characters)
        RUNNING_2 --> FAILED_2: API / Validation Error
        FAILED_2 --> RUNNING_2: User Retry Step 2
        RUNNING_2 --> CHARACTERS_GENERATED: Success (Max 2)
    }

    state CHARACTERS_GENERATED {
        [*] --> IDLE_2
        IDLE_2 --> RUNNING_3: Start Step 3 (Portraits)
        RUNNING_3 --> FAILED_3: Image API Error
        FAILED_3 --> RUNNING_3: User Retry Step 3
        RUNNING_3 --> PORTRAITS_GENERATED: Success
    }

    state PORTRAITS_GENERATED {
        [*] --> IDLE_3
        IDLE_3 --> RUNNING_4: Start Step 4 (Chapters)
        RUNNING_4 --> FAILED_4: API Error
        FAILED_4 --> RUNNING_4: User Retry Step 4
        RUNNING_4 --> CHAPTERS_GENERATED: Success (Max 1)
    }

    state CHAPTERS_GENERATED {
        [*] --> IDLE_4
        IDLE_4 --> RUNNING_5: Start Step 5 (Illustrations)
        RUNNING_5 --> FAILED_5: Multimodal Image Error
        FAILED_5 --> RUNNING_5: User Retry Step 5
        RUNNING_5 --> DONE: Success
    }

    state DONE {
        [*] --> COMPLETED: View / Read Mode
    }
```

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
