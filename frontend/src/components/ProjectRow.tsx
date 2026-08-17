import React from 'react';
import { ProjectStatus, ProjectSummary } from '../types';
import { getStatusIndex, PIPELINE_STEPS } from './Stepper';

interface ProjectRowProps {
  project: ProjectSummary;
  onClick: () => void;
  staggerIndex?: number;
}

export const ProjectRow: React.FC<ProjectRowProps> = ({ project, onClick, staggerIndex = 0 }) => {
  const completedCount = getStatusIndex(project.status);

  const getSubtitle = (status: ProjectStatus) => {
    if (status === 'CREATED') return 'Book text saved · style not yet generated';
    if (status === 'DONE') return 'All 5 steps complete';
    const doneSteps = PIPELINE_STEPS.slice(0, completedCount)
      .map((s) => s.label)
      .join(' + ');
    return `${doneSteps} done`;
  };

  const renderPill = (status: ProjectStatus) => {
    if (status === 'DONE') {
      return <span className="gd-pill ink">Done</span>;
    }
    if (status === 'CREATED') {
      return <span className="gd-pill gray">Draft</span>;
    }
    return (
      <span className="gd-pill">
        <span className="dot" />
        In progress
      </span>
    );
  };

  return (
    <div
      className="project-row"
      tabIndex={0}
      role="button"
      onClick={onClick}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onClick();
        }
      }}
      style={{ '--stagger': `${staggerIndex * 45}ms` } as React.CSSProperties}
    >
      <div className="title">
        <h4>{project.title}</h4>
        <span className="meta">
          Created {new Date(project.createdAt).toLocaleDateString()} · {getSubtitle(project.status)}
        </span>
      </div>

      <div className="progress-mini" aria-label={`Progress: ${completedCount} of 5 steps`}>
        {PIPELINE_STEPS.map((_, i) => (
          <span key={i} className={`seg ${i < completedCount ? 'on' : ''}`} />
        ))}
      </div>

      {renderPill(project.status)}
    </div>
  );
};
