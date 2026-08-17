import React, { useEffect, useState, useCallback } from 'react';
import { ProjectDetail, StepKey, User } from '../types';
import { api } from '../services/api';
import { Stepper, getStatusIndex, PIPELINE_STEPS } from '../components/Stepper';
import { EntityCard } from '../components/EntityCard';
import { BookModal } from '../components/BookModal';

interface ProjectDetailPageProps {
  projectId: string;
  user: User;
  onNavigate: (route: string) => void;
}

const STEP_CAPTIONS: Record<StepKey, string> = {
  STYLE: 'Reading your book text and defining an art style',
  CHARACTERS: 'Analyzing the book and generating the adult character prompts',
  PORTRAITS: 'Generating character portraits with Gemini',
  CHAPTERS: 'Generating structured chapter illustration prompt referencing characters',
  ILLUSTRATIONS: 'Generating scene illustration with multimodal character reference',
};

export const ProjectDetailPage: React.FC<ProjectDetailPageProps> = ({
  projectId,
  onNavigate,
}) => {
  const [project, setProject] = useState<ProjectDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [customStyle, setCustomStyle] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isBookModalOpen, setIsBookModalOpen] = useState(false);
  const [runningAction, setRunningAction] = useState(false);

  const fetchProject = useCallback(async (isPolling = false) => {
    if (!isPolling) setLoading(true);
    try {
      const data = await api.getProject(projectId);
      setProject(data);
      setError(data.lastError || null);
    } catch (err: any) {
      if (!isPolling) {
        setError(err.message || 'Failed to load project.');
      }
    } finally {
      if (!isPolling) setLoading(false);
    }
  }, [projectId]);

  useEffect(() => {
    fetchProject();
  }, [fetchProject]);

  // Polling mechanism while backend step is RUNNING
  useEffect(() => {
    if (!project || project.stepState !== 'RUNNING') return;

    const interval = setInterval(() => {
      fetchProject(true);
    }, 2500);

    return () => clearInterval(interval);
  }, [project, fetchProject]);

  const handleRunStep = async (stepKey: StepKey) => {
    setError(null);
    setRunningAction(true);
    try {
      const updated = await api.runStep(
        projectId,
        stepKey,
        stepKey === 'STYLE' ? customStyle : undefined
      );
      setProject(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message || `Failed to execute step ${stepKey}.`);
      fetchProject(true);
    } finally {
      setRunningAction(false);
    }
  };

  const handleResetStuck = async () => {
    setError(null);
    try {
      const updated = await api.resetStuck(projectId);
      setProject(updated);
      setError(null);
    } catch (err: any) {
      setError(err.message || 'Failed to reset stuck step.');
    }
  };

  if (loading) {
    return (
      <div className="app-body" style={{ textAlign: 'center', padding: '64px 0' }}>
        <span className="spinner" />
        <div className="meta" style={{ marginTop: '12px' }}>Loading project details…</div>
      </div>
    );
  }

  if (!project) {
    return (
      <div className="app-body">
        <a className="back-link" onClick={() => onNavigate('#/projects')}>
          ← Back to projects
        </a>
        <div className="empty-state">
          <p>Project not found.</p>
        </div>
      </div>
    );
  }

  const completedCount = getStatusIndex(project.status);
  const currentStep = PIPELINE_STEPS[completedCount]; // undefined when DONE
  const isRunning = project.stepState === 'RUNNING' || runningAction;
  const isFailed = project.stepState === 'FAILED';

  // Check if step is stuck for more than 45 seconds
  const isStuck =
    project.stepState === 'RUNNING' &&
    project.stepStartedAt &&
    Date.now() - new Date(project.stepStartedAt).getTime() > 45000;

  const portraitsGenerating = isRunning && currentStep?.key === 'PORTRAITS';
  const illustrationsGenerating = isRunning && currentStep?.key === 'ILLUSTRATIONS';

  return (
    <div className="app-body">
      <a className="back-link" onClick={() => onNavigate('#/projects')}>
        ← Back to projects
      </a>
      <h2 style={{ fontSize: '22px', marginBottom: '4px' }}>{project.title}</h2>
      <p className="meta" style={{ marginBottom: '24px' }}>
        Created {new Date(project.createdAt).toLocaleDateString()} · Status: <b>{project.status}</b>
      </p>

      <Stepper status={project.status} currentRunningStep={isRunning ? currentStep?.key : null} />

      <div className="detail-grid">
        <div>
          {/* Main Action / Status Panel */}
          <div className="step-panel">
            {!currentStep ? (
              <div>
                <div className="status-line" style={{ color: 'var(--grad-ink)' }}>
                  <span className="gd-num-square done" style={{ width: '20px', height: '20px', fontSize: '11px' }}>
                    ✓
                  </span>
                  All 5 steps complete — book illustration is fully generated!
                </div>
                <p className="help">
                  This project is complete. You can revisit anytime; nothing regenerates automatically.
                </p>
              </div>
            ) : isStuck ? (
              <div>
                <div className="status-line" style={{ color: 'var(--grad-orange-deep)' }}>
                  ⚠️ This step has been running longer than expected. It may have been interrupted.
                </div>
                <p className="help">
                  Previously completed steps are completely safe in storage. You can reset and retry.
                </p>
                <button
                  className="gd-btn gd-btn-secondary"
                  style={{ marginTop: '14px' }}
                  onClick={handleResetStuck}
                >
                  Reset & Retry {currentStep.label}
                </button>
              </div>
            ) : isFailed ? (
              <div>
                <div className="status-line" style={{ color: 'var(--grad-orange-deep)' }}>
                  ❌ Step <b>{currentStep.label}</b> failed: {error || project.lastError || 'Unknown error'}
                </div>
                <p className="help">
                  Completed milestones remain intact. You can retry this step immediately.
                </p>
                <button
                  className="gd-btn gd-btn-primary"
                  style={{ marginTop: '14px' }}
                  onClick={() => handleRunStep(currentStep.key)}
                  disabled={isRunning}
                >
                  {isRunning ? 'Retrying…' : `Retry ${currentStep.label}`}
                </button>
              </div>
            ) : isRunning ? (
              <div>
                <div className="status-line">
                  <span className="spinner" />
                  {STEP_CAPTIONS[currentStep.key]}…
                </div>
                <p className="help">
                  Reopening this page or refreshing will not trigger duplicate calls — it resumes the in-flight state.
                </p>
                <button className="gd-btn gd-btn-primary" style={{ marginTop: '14px' }} disabled>
                  Generating {currentStep.label}…
                </button>
              </div>
            ) : (
              <div>
                <div className="status-line" style={{ color: 'var(--grad-ink)' }}>
                  Ready for the next step: <b>{currentStep.label}</b>.
                </div>

                {currentStep.key === 'STYLE' && (
                  <div className="gd-field" style={{ marginBottom: '14px' }}>
                    <label htmlFor="style-input">Art style (optional)</label>
                    <input
                      id="style-input"
                      placeholder="Leave blank to let Gemini define a fitting style based on your book"
                      value={customStyle}
                      onChange={(e) => setCustomStyle(e.target.value)}
                    />
                  </div>
                )}

                <p className="help">
                  Each step executes independently and stores its state securely.
                </p>

                <button
                  className="gd-btn gd-btn-primary"
                  style={{ marginTop: '14px' }}
                  onClick={() => handleRunStep(currentStep.key)}
                >
                  Generate {currentStep.label} <span className="gd-arrow">→</span>
                </button>
              </div>
            )}
          </div>

          {/* Dynamic Entities Grid */}
          <div style={{ marginTop: '28px' }}>
            {/* Chapters */}
            {project.chapters.length > 0 && (
              <div style={{ marginBottom: '28px' }}>
                <div className="panel-title">
                  <h3>Chapters ({project.chapters.length})</h3>
                </div>
                <div className="entity-grid" style={{ gridTemplateColumns: '1fr' }}>
                  {project.chapters.map((ch, idx) => (
                    <EntityCard
                      key={ch.id}
                      type="chapter"
                      name={ch.name}
                      prompt={ch.prompt}
                      imageUrl={ch.illustrationUrl}
                      isReady={ch.illustrationReady}
                      isGenerating={illustrationsGenerating}
                      staggerIndex={idx}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* Characters */}
            {project.characters.length > 0 && (
              <div>
                <div className="panel-title">
                  <h3>Characters ({project.characters.length})</h3>
                </div>
                <div className="entity-grid">
                  {project.characters.map((char, idx) => (
                    <EntityCard
                      key={char.id}
                      type="character"
                      name={char.name}
                      prompt={char.prompt}
                      imageUrl={char.portraitUrl}
                      isReady={char.portraitReady}
                      isGenerating={portraitsGenerating}
                      staggerIndex={idx}
                    />
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        {/* Side Note */}
        <div>
          {project.style ? (
            <div className="side-note">
              <h5>Art Style</h5>
              <p>{project.style}</p>
            </div>
          ) : (
            <div className="side-note">
              <h5>Book Text</h5>
              <p style={{ fontStyle: 'italic' }}>
                {project.bookText.length > 200
                  ? `${project.bookText.slice(0, 200)}…`
                  : project.bookText}
              </p>
              {project.bookText.length > 200 && (
                <button
                  type="button"
                  className="gd-btn gd-btn-ghost gd-btn-sm"
                  style={{ marginTop: '8px' }}
                  onClick={() => setIsBookModalOpen(true)}
                >
                  Read full text →
                </button>
              )}
            </div>
          )}

          {project.style && (
            <div className="side-note" style={{ marginTop: '16px' }}>
              <h5>Book Content</h5>
              <p style={{ fontStyle: 'italic' }}>
                {project.bookText.length > 120
                  ? `${project.bookText.slice(0, 120)}…`
                  : project.bookText}
              </p>
              <button
                type="button"
                className="gd-btn gd-btn-ghost gd-btn-sm"
                style={{ marginTop: '8px' }}
                onClick={() => setIsBookModalOpen(true)}
              >
                Read full text →
              </button>
            </div>
          )}
        </div>
      </div>

      <BookModal
        isOpen={isBookModalOpen}
        title={project.title}
        bookText={project.bookText}
        onClose={() => setIsBookModalOpen(false)}
      />
    </div>
  );
};
