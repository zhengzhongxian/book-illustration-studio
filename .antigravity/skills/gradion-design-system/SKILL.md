---
name: gradion-design-system
description: Design system guidelines, CSS custom properties, responsive layout tokens, and atom components matching the Gradion specification.
---

# Gradion Design System Guide

## Color & Typography Tokens

```css
:root {
  --grad-orange:        #FF6B00;
  --grad-orange-hover:  #E85F00;
  --grad-orange-light:  #FFA861;
  --grad-orange-pale:   #FFC391;
  --grad-orange-deep:   #3A160A;

  --grad-ink:           #231F20;
  --grad-ink-body:      #434343;
  --grad-ink-2:         #595959;
  --grad-ink-3:         #919699;
  --grad-line:          #BAB7B1;
  --grad-paper:         #F2EEE7;
  --grad-paper-2:       #F8F8F8;
  --grad-white:         #FFFFFF;
  --grad-black:         #1D1C1D;

  --font-sans:          "Noto Sans", system-ui, sans-serif;
  --font-display:       "Noto Sans", system-ui, sans-serif;
}
```

## Essential Components

1. **Stepper**:
   - 5 numbered circles (`1` to `5`) or checkmark (`✓`).
   - Active step has glowing pulse animation (`gd-ring-pulse`).
   - Connectors transition to orange when preceding step is completed.

2. **Entity Card**:
   - Aspect ratio `3/4` for Character portraits.
   - Aspect ratio `16/10` for Chapter illustrations.
   - State indicator: Spinner during generation, rendered image when ready, placeholder when pending.

3. **Status Pill**:
   - `Draft` (`.gray`), `In progress` (`.gd-pill` with pulsing dot), `Done` (`.ink`).

4. **Book Text Modal**:
   - Accessible dialog (`role="dialog"`, ESC key listener, focus trapping & restoration).
   - Displays full book text at any pipeline step.
