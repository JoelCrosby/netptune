export type BrandingTarget =
  | { kind: 'workspaceLogo' }
  | { kind: 'projectLogo'; projectId: number }
  | { kind: 'boardLogo'; boardId: number }
  | { kind: 'boardBackground'; boardId: number };

export interface BrandingImage {
  fileId: string;
  contentUrl: string;
  sizeBytes: number;
}
