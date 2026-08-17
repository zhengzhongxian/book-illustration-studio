export type ProjectStatus =
  | 'CREATED'
  | 'STYLE_SET'
  | 'CHARACTERS_GENERATED'
  | 'PORTRAITS_GENERATED'
  | 'CHAPTERS_GENERATED'
  | 'DONE';

export type StepState = 'IDLE' | 'RUNNING' | 'FAILED';

export type StepKey = 'STYLE' | 'CHARACTERS' | 'PORTRAITS' | 'CHAPTERS' | 'ILLUSTRATIONS';

export interface User {
  id: string;
  email: string;
  name: string;
  createdAt: string;
}

export interface Character {
  id: string;
  name: string;
  prompt: string;
  portraitUrl: string | null;
  portraitReady: boolean;
  sortOrder: number;
}

export interface Chapter {
  id: string;
  name: string;
  prompt: string;
  characters: string[];
  illustrationUrl: string | null;
  illustrationReady: boolean;
  sortOrder: number;
}

export interface ProjectSummary {
  id: string;
  title: string;
  status: ProjectStatus;
  stepState: StepState;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectDetail {
  id: string;
  userId: string;
  title: string;
  bookText: string;
  status: ProjectStatus;
  stepState: StepState;
  lastError: string | null;
  stepStartedAt: string | null;
  style: string | null;
  createdAt: string;
  updatedAt: string;
  characters: Character[];
  chapters: Chapter[];
}

export interface ApiResponse<T> {
  success: boolean;
  statusCode: number;
  data: T;
  message?: string | null;
  errors?: string[] | null;
  timestamp: string;
}
