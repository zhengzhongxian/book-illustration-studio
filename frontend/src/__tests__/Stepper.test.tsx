import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { Stepper } from '../components/Stepper';

describe('Stepper Component', () => {
  it('renders all 5 step labels', () => {
    render(<Stepper status="CREATED" />);

    expect(screen.getByText('Style')).toBeInTheDocument();
    expect(screen.getByText('Characters')).toBeInTheDocument();
    expect(screen.getByText('Portraits')).toBeInTheDocument();
    expect(screen.getByText('Chapters')).toBeInTheDocument();
    expect(screen.getByText('Illustrations')).toBeInTheDocument();
  });

  it('marks completed steps with checkmark and active step with number', () => {
    const { container } = render(<Stepper status="CHARACTERS_GENERATED" />);

    // First 2 steps are done (Style, Characters) -> have checkmarks
    const doneCheckmarks = screen.getAllByLabelText(/Done/i);
    expect(doneCheckmarks).toHaveLength(2);

    // Step 3 (Portraits) is current
    const currentStep = container.querySelector('.step.current');
    expect(currentStep).toBeInTheDocument();
    expect(currentStep?.textContent).toContain('Portraits');
  });

  it('marks all steps done when status is DONE', () => {
    render(<Stepper status="DONE" />);
    const doneCheckmarks = screen.getAllByLabelText(/Done/i);
    expect(doneCheckmarks).toHaveLength(5);
  });
});
