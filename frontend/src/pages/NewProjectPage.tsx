import React, { useState } from 'react';
import { User } from '../types';
import { api } from '../services/api';
import { Dropzone } from '../components/Dropzone';

interface NewProjectPageProps {
  user: User;
  onNavigate: (route: string) => void;
}

export const NewProjectPage: React.FC<NewProjectPageProps> = ({ user, onNavigate }) => {
  const [title, setTitle] = useState('');
  const [bookText, setBookText] = useState('');
  const [fileName, setFileName] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleFileLoaded = (name: string, content: string) => {
    setFileName(name);
    setBookText(content);
    if (!title) {
      const cleanTitle = name.replace(/\.txt$/i, '').replace(/[-_]/g, ' ');
      setTitle(cleanTitle);
    }
  };

  const handleCreate = async () => {
    setError(null);
    const trimmedTitle = title.trim();
    const trimmedText = bookText.trim();

    if (!trimmedTitle) {
      setError('Give your project a title.');
      return;
    }

    if (!trimmedText || trimmedText.length < 10) {
      setError('Please provide book text (paste text or upload a .txt file).');
      return;
    }

    setLoading(true);
    try {
      const created = await api.createProject(trimmedTitle, trimmedText, user.id);
      onNavigate(`#/projects/${created.id}`);
    } catch (err: any) {
      setError(err.message || 'Failed to create project.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="app-body narrow">
      <a className="back-link" onClick={() => onNavigate('#/projects')}>
        ← Back to projects
      </a>
      <h3 style={{ fontSize: '20px' }}>Start a new illustration project</h3>
      <p className="meta" style={{ marginBottom: '20px' }}>
        Give it a title, then paste the book's text or upload a .txt file.
      </p>

      <div className="gd-field">
        <label htmlFor="f-title">
          Project title <span className="req">*</span>
        </label>
        <input
          id="f-title"
          placeholder="e.g. The Wind in the Willows — cottage-core"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          disabled={loading}
        />
      </div>

      <div className="gd-field" style={{ marginTop: '16px' }}>
        <label htmlFor="book-textarea">
          Book text <span className="req">*</span>
        </label>
        <Dropzone fileName={fileName} onFileLoaded={handleFileLoaded} />
        <div className="divider-or">or paste text</div>
        <textarea
          id="book-textarea"
          rows={6}
          placeholder="Once upon a time, in a small burrow by the river..."
          value={bookText}
          onChange={(e) => setBookText(e.target.value)}
          disabled={loading}
        />
      </div>

      {error && <div className="gd-field err">{error}</div>}

      <button
        className="gd-btn gd-btn-primary"
        style={{ width: '100%', justifyContent: 'center', marginTop: '20px' }}
        onClick={handleCreate}
        disabled={loading}
      >
        {loading ? 'Creating…' : 'Create project'} <span className="gd-arrow">→</span>
      </button>
    </div>
  );
};
