import React, { useEffect, useState } from 'react';
import { ProjectSummary, User } from '../types';
import { api } from '../services/api';
import { ProjectRow } from '../components/ProjectRow';

interface ProjectListPageProps {
  user: User;
  onNavigate: (route: string) => void;
}

export const ProjectListPage: React.FC<ProjectListPageProps> = ({ user, onNavigate }) => {
  const [projects, setProjects] = useState<ProjectSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchProjects = async () => {
    setLoading(true);
    setError(null);
    try {
      const list = await api.getUserProjects(user.id);
      setProjects(list);
    } catch (err: any) {
      setError(err.message || 'Failed to load projects.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchProjects();
  }, [user.id]);

  return (
    <div className="app-body">
      <div className="list-head">
        <h2>Your projects</h2>
        <button
          className="gd-btn gd-btn-primary"
          onClick={() => onNavigate('#/projects/new')}
        >
          + New project
        </button>
      </div>

      {loading ? (
        <div style={{ textAlign: 'center', padding: '48px 0' }}>
          <span className="spinner" />
          <div className="meta" style={{ marginTop: '8px' }}>Loading projects…</div>
        </div>
      ) : error ? (
        <div className="step-panel" style={{ borderColor: 'var(--grad-orange)' }}>
          <div className="status-line" style={{ color: 'var(--grad-ink)' }}>
            ⚠️ {error}
          </div>
          <button className="gd-btn gd-btn-secondary gd-btn-sm" onClick={fetchProjects}>
            Retry
          </button>
        </div>
      ) : projects.length === 0 ? (
        <div className="empty-state">
          <p style={{ margin: 0 }}>No projects yet.</p>
          <button
            className="gd-btn gd-btn-primary"
            onClick={() => onNavigate('#/projects/new')}
          >
            + New project
          </button>
        </div>
      ) : (
        <div className="project-list">
          {projects.map((p, index) => (
            <ProjectRow
              key={p.id}
              project={p}
              onClick={() => onNavigate(`#/projects/${p.id}`)}
              staggerIndex={index}
            />
          ))}
        </div>
      )}
    </div>
  );
};
