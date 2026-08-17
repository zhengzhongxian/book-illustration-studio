import React from 'react';
import { api } from '../services/api';

interface EntityCardProps {
  type: 'character' | 'chapter';
  name: string;
  prompt: string;
  imageUrl: string | null;
  isReady: boolean;
  isGenerating?: boolean;
  staggerIndex?: number;
}

export const EntityCard: React.FC<EntityCardProps> = ({
  type,
  name,
  prompt,
  imageUrl,
  isReady,
  isGenerating = false,
  staggerIndex = 0,
}) => {
  const artClass = type === 'character' ? 'art' : 'art chapter';
  const fullImageUrl = api.getImageUrl(imageUrl);

  let artContent: React.ReactNode;

  if (isReady && fullImageUrl) {
    artContent = (
      <div className={artClass}>
        <img src={fullImageUrl} alt={name} loading="lazy" />
      </div>
    );
  } else if (isGenerating) {
    artContent = (
      <div className={`${artClass} pending`}>
        <div style={{ textAlign: 'center' }}>
          <span className="spinner" />
          <div className="gen-caption">
            Generating {type === 'character' ? `portrait for ${name}` : 'scene illustration'}…
          </div>
        </div>
      </div>
    );
  } else {
    artContent = (
      <div className={`${artClass} pending`}>
        <span className="placeholder-label muted">Not generated yet</span>
      </div>
    );
  }

  return (
    <div
      className="entity-card"
      style={{ '--stagger': `${staggerIndex * 60}ms` } as React.CSSProperties}
    >
      {artContent}
      <div className="body">
        <h5>{name}</h5>
        <p>{prompt}</p>
      </div>
    </div>
  );
};
