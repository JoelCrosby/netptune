export type ArchiveImportMode = 'clone' | 'restore';

export interface ArchiveImportPreview {
  schemaVersion: number;
  workspaceName: string;
  workspaceSlug: string;
  createdAt: string;
  countsByType: Record<string, number>;
  unmatchedMemberEmails: string[];
  fileBytes: number;
  remainingQuotaBytes: number;
  schemaUpgrades: string[];
  blockers: string[];
}

export interface ArchiveImportResult {
  workspaceId: number;
  workspaceSlug: string;
  createdByType: Record<string, number>;
  warnings: string[];
}
