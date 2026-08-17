import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { ProjectRow } from '../components/ProjectRow';
import { ProjectSummary } from '../types';

describe('ProjectRow Component', () => {
  const mockProject: ProjectSummary = {
    id: 'proj-1',
    title: 'The Wind in the Willows',
    status: 'PORTRAITS_GENERATED',
    stepState: 'IDLE',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };

  it('renders title, progress, and in-progress pill', () => {
    const handleClick = vi.fn();
    render(<ProjectRow project={mockProject} onClick={handleClick} />);

    expect(screen.getByText('The Wind in the Willows')).toBeInTheDocument();
    expect(screen.getByText(/Style \+ Characters \+ Portraits done/i)).toBeInTheDocument();
    expect(screen.getByText('In progress')).toBeInTheDocument();
  });

  it('renders Done pill when project is completed', () => {
    const doneProject: ProjectSummary = {
      ...mockProject,
      status: 'DONE',
    };

    render(<ProjectRow project={doneProject} onClick={() => {}} />);
    expect(screen.getByText('Done')).toBeInTheDocument();
    expect(screen.getByText(/All 5 steps complete/i)).toBeInTheDocument();
  });
});
