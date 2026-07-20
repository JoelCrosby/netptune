export enum WorkspaceFilePurpose {
  taskFile = 0,
  inlineMedia = 1,
}

export interface WorkspaceFileViewModel {
  id: number;
  originalName: string;
  contentType: string;
  contentTypeGroup: 'image' | 'document' | 'archive' | 'other';
  sizeBytes: number;
  purpose: WorkspaceFilePurpose;
  createdAt: string;
  uploadedByUserId?: string;
  uploadedByDisplayName?: string;
  uploadedByPictureUrl?: string;
  uploadedByIsServiceAccount?: boolean;
  taskId?: number;
  taskSystemId?: string;
  taskName?: string;
  contentUrl: string;
  canDelete: boolean;
}

export interface WorkspaceStorageUsage {
  usedBytes: number;
  limitBytes: number;
  availableBytes: number;
  percentage: number;
  fileCount: number;
}

export interface FileUploadResult {
  fileName: string;
  isSuccess: boolean;
  file?: WorkspaceFileViewModel;
  error?: string;
}

export interface WorkspaceFileFilter {
  page?: number;
  pageSize?: number;
  query?: string;
  purpose?: WorkspaceFilePurpose;
  contentTypeGroup?: string;
  sortBy?: 'createdAt' | 'name' | 'sizeBytes';
  sortDirection?: 'asc' | 'desc';
}
