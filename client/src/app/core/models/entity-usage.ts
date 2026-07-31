export enum UsageSubjectKind {
  status = 0,
  tag = 1,
  relationType = 2,
}

export enum UsageReferenceKind {
  project = 0,
  boardGroup = 1,
  automationRule = 2,
}

export interface UsageReference {
  id: number;
  name: string;
  context?: string | null;
}

export interface UsageReferenceGroup {
  kind: UsageReferenceKind;
  items: UsageReference[];
}

export interface EntityUsage {
  id: number;
  kind: UsageSubjectKind;
  name: string;
  usageCount: number;
  references: UsageReferenceGroup[];
  canDelete: boolean;
  blockedReason?: string | null;
}
