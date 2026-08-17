import React from 'react';
import { User } from '../types';

interface NavProps {
  user: User | null;
  onNavigate: (route: string) => void;
  onSignOut: () => void;
}

export const Nav: React.FC<NavProps> = ({ user, onNavigate, onSignOut }) => {
  if (!user) return null;

  const initials = (user.name || '?')
    .split(' ')
    .map((w) => w[0])
    .join('')
    .slice(0, 2)
    .toUpperCase();

  return (
    <div className="gd-nav">
      <div className="gd-nav-inner">
        <div className="gd-nav-logo" onClick={() => onNavigate('#/projects')}>
          <span>GRADION</span> <b>STUDIO</b>
        </div>
        <div className="gd-nav-links">
          <a onClick={() => onNavigate('#/projects')}>Projects</a>
        </div>
        <div className="gd-nav-user">
          <div className="gd-nav-avatar">{initials}</div>
          <span>{user.name}</span>
          <a onClick={onSignOut}>Sign out</a>
        </div>
      </div>
    </div>
  );
};
