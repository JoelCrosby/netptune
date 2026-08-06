export enum ExportJobStatus {
  pending = 0,
  running = 1,
  succeeded = 2,
  failed = 3,
  cancelled = 4,
  expired = 5,
}

export enum ExportFormat {
  csv = 0,
  tsv = 1,
  xlsx = 2,
  json = 3,
  ndjson = 4,
  archive = 5,
}

export interface ExportJobViewModel {
  publicId: string;
  status: ExportJobStatus;
  recordType: string;
  format: ExportFormat;
  name?: string;
  fileName?: string;
  rowCount?: number;
  sizeBytes?: number;
  progressPercent: number;
  progressMessage?: string;
  error?: string;
  hasArtefact: boolean;
  requestedByUserId?: string;
  requestedByDisplayName?: string;
  requestedByPictureUrl?: string;
  createdAt: string;
  startedAt?: string;
  completedAt?: string;
  expiresAt: string;
}

export interface ExportJobProgressEvent {
  publicId: string;
  status: ExportJobStatus;
  progressPercent: number;
  progressMessage?: string;
  error?: string;
}
