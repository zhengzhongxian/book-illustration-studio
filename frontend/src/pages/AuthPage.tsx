import React, { useState } from 'react';
import { User } from '../types';
import { api } from '../services/api';

interface AuthPageProps {
  onSignInSuccess: (user: User) => void;
}

export const AuthPage: React.FC<AuthPageProps> = ({ onSignInSuccess }) => {
  const [name, setName] = useState('');
  const [email, setEmail] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    setError(null);

    const trimmedName = name.trim();
    const trimmedEmail = email.trim().toLowerCase();

    if (!trimmedName || !trimmedEmail || !trimmedEmail.includes('@')) {
      setError('Enter your name and a valid email address to continue.');
      return;
    }

    setLoading(true);
    try {
      const user = await api.signIn(trimmedName, trimmedEmail);
      onSignInSuccess(user);
    } catch (err: any) {
      setError(err.message || 'Failed to sign in. Please check backend connection.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="center-page">
      <div className="auth-card">
        <div className="logo-row">
          <h2 style={{ color: 'var(--grad-orange)', margin: 0, fontSize: '24px' }}>
            <span style={{ color: 'var(--grad-ink)' }}>GRADION</span> STUDIO
          </h2>
        </div>
        <h3 style={{ textAlign: 'center', fontSize: '20px' }}>Book Illustration Studio</h3>
        <p className="lede">Enter your details to start or resume an illustration project.</p>

        <form onSubmit={handleSubmit}>
          <div className="gd-field">
            <label htmlFor="f-name">
              Full name <span className="req">*</span>
            </label>
            <input
              id="f-name"
              placeholder="e.g. Mira Hassan"
              value={name}
              onChange={(e) => setName(e.target.value)}
              disabled={loading}
            />
          </div>

          <div className="gd-field">
            <label htmlFor="f-email">
              Email <span className="req">*</span>
            </label>
            <input
              id="f-email"
              type="email"
              placeholder="e.g. mira@example.com"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={loading}
            />
          </div>

          {error && <div className="gd-field err">{error}</div>}

          <button
            type="submit"
            className="gd-btn gd-btn-primary"
            style={{ width: '100%', justifyContent: 'center', marginTop: '20px' }}
            disabled={loading}
          >
            {loading ? 'Continuing…' : 'Continue'} <span className="gd-arrow">→</span>
          </button>
        </form>

        <p className="meta" style={{ textAlign: 'center', marginTop: '16px' }}>
          No password — using an email that already has projects resumes them exactly where you left off.
        </p>
      </div>
    </div>
  );
};
