import { render, screen } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { EntityCard } from '../components/EntityCard';

describe('EntityCard Component', () => {
  it('renders placeholder state when image is not ready', () => {
    render(
      <EntityCard
        type="character"
        name="Mole"
        prompt="A friendly velvet-coated mole."
        imageUrl={null}
        isReady={false}
      />
    );

    expect(screen.getByText('Mole')).toBeInTheDocument();
    expect(screen.getByText('A friendly velvet-coated mole.')).toBeInTheDocument();
    expect(screen.getByText('Not generated yet')).toBeInTheDocument();
  });

  it('renders loading spinner when isGenerating is true', () => {
    render(
      <EntityCard
        type="character"
        name="Mole"
        prompt="A friendly mole."
        imageUrl={null}
        isReady={false}
        isGenerating={true}
      />
    );

    expect(screen.getByText(/Generating portrait for Mole/i)).toBeInTheDocument();
  });

  it('renders image when isReady is true and imageUrl is present', () => {
    render(
      <EntityCard
        type="character"
        name="Ratty"
        prompt="A river rat."
        imageUrl="/api/images/portraits/123"
        isReady={true}
      />
    );

    const img = screen.getByRole('img');
    expect(img).toHaveAttribute('alt', 'Ratty');
    expect(img).toHaveAttribute('src', 'http://localhost:5000/api/images/portraits/123');
  });
});
