import React from 'react';
import { ProjectStatus, StepKey } from '../types';

export interface StepDefinition {
  key: StepKey;
  label: string;
  targetStatus: ProjectStatus;
}

export const PIPELINE_STEPS: StepDefinition[] = [
  { key: 'STYLE', label: 'Style', targetStatus: 'STYLE_SET' },
  { key: 'CHARACTERS', label: 'Characters', targetStatus: 'CHARACTERS_GENERATED' },
  { key: 'PORTRAITS', label: 'Portraits', targetStatus: 'PORTRAITS_GENERATED' },
  { key: 'CHAPTERS', label: 'Chapters', targetStatus: 'CHAPTERS_GENERATED' },
  { key: 'ILLUSTRATIONS', label: 'Illustrations', targetStatus: 'DONE' },
];

export const STATUS_ORDER: ProjectStatus[] = [
  'CREATED',
  'STYLE_SET',
  'CHARACTERS_GENERATED',
  'PORTRAITS_GENERATED',
  'CHAPTERS_GENERATED',
  'DONE',
];

export function getStatusIndex(status: ProjectStatus): number {
  return STATUS_ORDER.indexOf(status);
}

interface StepperProps {
  status: ProjectStatus;
  currentRunningStep?: StepKey | null;
}

export const Stepper: React.FC<StepperProps> = ({ status }) => {
  const completedIndex = getStatusIndex(status); // 0..5

  return (
    <div className="stepper" role="navigation" aria-label="Pipeline Progress">
      {PIPELINE_STEPS.map((step, index) => {
        const isDone = index < completedIndex;
        const isCurrent = index === completedIndex;
        const cls = isDone ? 'done' : isCurrent ? 'current' : 'pending';

        return (
          <React.Fragment key={step.key}>
            <div className={`step ${cls}`}>
              {isDone ? (
                <span className="gd-num-square done" aria-label={`Step ${index + 1} Done`}>
                  ✓
                </span>
              ) : (
                <span className={`gd-num-square ${isCurrent ? '' : 'gray'}`}>
                  {index + 1}
                </span>
              )}
              <span className="lbl">{step.label}</span>
            </div>
            {index < PIPELINE_STEPS.length - 1 && (
              <div className={`connector ${index < completedIndex ? 'done' : ''}`} />
            )}
          </React.Fragment>
        );
      })}
    </div>
  );
};
