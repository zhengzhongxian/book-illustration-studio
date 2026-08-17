import { ApiResponse, ProjectDetail, ProjectSummary, StepKey, User } from '../types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api';

class ApiClient {
  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${API_BASE_URL}${endpoint}`;
    const headers = {
      'Content-Type': 'application/json',
      ...options.headers,
    };

    const response = await fetch(url, {
      ...options,
      headers,
    });

    const body: ApiResponse<T> = await response.json().catch(() => ({
      success: false,
      statusCode: response.status,
      data: null as unknown as T,
      message: `Network response failed with status ${response.status}`,
      timestamp: new Date().toISOString(),
    }));

    if (!response.ok || !body.success) {
      const errorMsg = body.message || (body.errors && body.errors[0]) || `Error ${response.status}`;
      const err = new Error(errorMsg);
      (err as any).statusCode = response.status;
      (err as any).isConflict = response.status === 409;
      throw err;
    }

    return body.data;
  }

  async signIn(name: string, email: string): Promise<User> {
    return this.request<User>('/auth/signin', {
      method: 'POST',
      body: JSON.stringify({ name, email }),
    });
  }

  async getUserProjects(userId: string): Promise<ProjectSummary[]> {
    return this.request<ProjectSummary[]>(`/projects?userId=${encodeURIComponent(userId)}`);
  }

  async getProject(projectId: string): Promise<ProjectDetail> {
    return this.request<ProjectDetail>(`/projects/${projectId}`);
  }

  async createProject(title: string, bookText: string, userId: string): Promise<ProjectDetail> {
    return this.request<ProjectDetail>('/projects', {
      method: 'POST',
      body: JSON.stringify({ title, bookText, userId }),
    });
  }

  async deleteProject(projectId: string, userId: string): Promise<void> {
    await this.request<void>(`/projects/${projectId}?userId=${encodeURIComponent(userId)}`, {
      method: 'DELETE',
    });
  }

  async runStep(projectId: string, stepKey: StepKey, customStyle?: string): Promise<ProjectDetail> {
    return this.request<ProjectDetail>(`/projects/${projectId}/pipeline/steps/${stepKey}`, {
      method: 'POST',
      body: JSON.stringify({ customStyle: customStyle || null }),
    });
  }

  async resetStuck(projectId: string): Promise<ProjectDetail> {
    return this.request<ProjectDetail>(`/projects/${projectId}/pipeline/reset-stuck`, {
      method: 'POST',
    });
  }

  getImageUrl(relativePathOrUrl: string | null): string | null {
    if (!relativePathOrUrl) return null;
    if (relativePathOrUrl.startsWith('http://') || relativePathOrUrl.startsWith('https://')) {
      return relativePathOrUrl;
    }
    const cleanPath = relativePathOrUrl.startsWith('/') ? relativePathOrUrl.slice(1) : relativePathOrUrl;
    // Base url without trailing /api
    const serverHost = API_BASE_URL.replace(/\/api\/?$/, '');
    return `${serverHost}/${cleanPath}`;
  }
}

export const api = new ApiClient();
