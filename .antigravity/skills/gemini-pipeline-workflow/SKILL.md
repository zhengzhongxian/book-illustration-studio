---
name: gemini-pipeline-workflow
description: Guides the implementation and validation of the 5-step Gemini illustration pipeline from text to portraits and chapter illustrations.
---

# Gemini Illustration Pipeline Workflow

## Overview
This skill defines the technical specification and execution workflow for the 5-step Book Illustration Pipeline based on Google Gemini REST APIs.

## 5-Step Pipeline Reference Contract

### Step 1: Style Definition (`STYLE`)
- **Input**: User-provided style string or empty string.
- **Process**:
  - If user provides style: Record style as fixed prompt instruction.
  - If empty: Prompt Gemini (`gemini-2.5-flash`) to generate a fitting art style with a twist.
- **Output**: Art style string (stored in project entity).
- **Status Transition**: `CREATED` -> `STYLE_SET`.

### Step 2: Extract Adult Characters (`CHARACTERS`)
- **Input**: Book text content + established Art Style.
- **Constraints**: Hard cap of **maximum 2 adult characters** (`max_character_images = 2`).
- **Structured JSON Schema**:
  ```json
  {
    "type": "array",
    "items": {
      "type": "object",
      "properties": {
        "name": { "type": "string" },
        "prompt": { "type": "string", "description": "At least 50 words description" }
      },
      "required": ["name", "prompt"]
    }
  }
  ```
- **Output**: Array of up to 2 Character entities.
- **Status Transition**: `STYLE_SET` -> `CHARACTERS_GENERATED`.

### Step 3: Generate Character Portraits (`PORTRAITS`)
- **Input**: Characters generated in Step 2.
- **Image Generation Config**:
  - Model: `gemini-2.5-flash-image` (or `imagen-3.0-generate-002`).
  - Aspect Ratio: `9:16` (portrait).
  - System Negative Prompt: No text, no cover borders, family-friendly, single character portrait.
- **Output**: Base64 image saved to local storage (`/storage/portraits/{id}.png`), mapped to `Character.PortraitUrl` and `PortraitReady = true`.
- **Status Transition**: `CHARACTERS_GENERATED` -> `PORTRAITS_GENERATED`.

### Step 4: Generate Chapter Prompts (`CHAPTERS`)
- **Input**: Book text + Style + Character names & descriptions.
- **Constraints**: Hard cap of **maximum 1 chapter illustration** (`max_chapter_images = 1`).
- **Structured JSON Schema**:
  ```json
  {
    "type": "array",
    "items": {
      "type": "object",
      "properties": {
        "name": { "type": "string" },
        "prompt": { "type": "string" },
        "characters": { "type": "array", "items": { "type": "string" } }
      },
      "required": ["name", "prompt", "characters"]
    }
  }
  ```
- **Output**: Array of up to 1 Chapter entity.
- **Status Transition**: `PORTRAITS_GENERATED` -> `CHAPTERS_GENERATED`.

### Step 5: Generate Chapter Illustrations (`ILLUSTRATIONS`)
- **Input**: Chapter prompt + Multimodal character reference images (from Step 3).
- **Process**: Pass the portrait image bytes as inline data (`image/png`) to Gemini alongside the chapter scene prompt for visual consistency.
- **Output**: Saved image (`/storage/illustrations/{id}.png`), mapped to `Chapter.IllustrationUrl` and `IllustrationReady = true`.
- **Status Transition**: `CHAPTERS_GENERATED` -> `DONE`.
