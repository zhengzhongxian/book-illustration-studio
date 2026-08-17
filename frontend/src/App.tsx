import { useEffect, useState } from 'react';
import { User } from './types';
import { Nav } from './components/Nav';
import { AuthPage } from './pages/AuthPage';
import { ProjectListPage } from './pages/ProjectListPage';
import { NewProjectPage } from './pages/NewProjectPage';
import { ProjectDetailPage } from './pages/ProjectDetailPage';

const USER_STORAGE_KEY = 'bi_studio_user';

export function App() {
  const [user, setUser] = useState<User | null>(() => {
    try {
      const stored = localStorage.getItem(USER_STORAGE_KEY);
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  });

  const [currentHash, setCurrentHash] = useState<string>(window.location.hash || '#/');

  useEffect(() => {
    const handleHashChange = () => {
      setCurrentHash(window.location.hash || '#/');
    };
    window.addEventListener('hashchange', handleHashChange);
    return () => window.removeEventListener('hashchange', handleHashChange);
  }, []);

  const navigate = (hash: string) => {
    window.location.hash = hash;
    setCurrentHash(hash);
  };

  const handleSignIn = (signedInUser: User) => {
    setUser(signedInUser);
    localStorage.setItem(USER_STORAGE_KEY, JSON.stringify(signedInUser));
    navigate('#/projects');
  };

  const handleSignOut = () => {
    setUser(null);
    localStorage.removeItem(USER_STORAGE_KEY);
    navigate('#/');
  };

  // Route resolver
  const cleanRoute = currentHash.replace(/^#\/?/, '');

  let content: React.ReactNode;

  if (!user) {
    content = <AuthPage onSignInSuccess={handleSignIn} />;
  } else if (cleanRoute === '' || cleanRoute === 'projects') {
    content = <ProjectListPage user={user} onNavigate={navigate} />;
  } else if (cleanRoute === 'projects/new') {
    content = <NewProjectPage user={user} onNavigate={navigate} />;
  } else if (cleanRoute.startsWith('projects/')) {
    const projectId = cleanRoute.replace('projects/', '');
    content = <ProjectDetailPage projectId={projectId} user={user} onNavigate={navigate} />;
  } else {
    content = <ProjectListPage user={user} onNavigate={navigate} />;
  }

  return (
    <div>
      {user && <Nav user={user} onNavigate={navigate} onSignOut={handleSignOut} />}

      <main>{content}</main>

      <footer className="app-footer">
        <span className="gd-signature">
          GRADION <b>|</b> Scaling Business
        </span>
      </footer>
    </div>
  );
}

export default App;
