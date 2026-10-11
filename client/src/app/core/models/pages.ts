import { StatusCategory } from '@core/models/status';

export enum PageKind {
  folder = 0,
  document = 1,
}

export enum PageStatus {
  draft = 0,
  published = 1,
}

export interface PageTreeNode {
  id: number;
  parentId: number | null;
  projectId?: number | null;
  kind: PageKind;
  title: string;
  slug: string;
  icon: string | null;
  sortOrder: number;
  status: PageStatus;
  isFavourite: boolean;
}

export interface PageDetail {
  id: number;
  parentId: number | null;
  projectId: number | null;
  kind: PageKind;
  title: string;
  slug: string;
  icon: string | null;
  body: string | null;
  status: PageStatus;
  revision: number;
  isFavourite: boolean;
  createdAt: string;
  updatedAt: string | null;
  createdByUserId: string | null;
  createdByDisplayName: string | null;
  modifiedByDisplayName: string | null;
  tags: PageTag[];
  contributors: PageContributor[];
}

export interface PageContributor {
  userId: string;
  displayName: string;
  pictureUrl: string | null;
  lastEditedAt: string;
}

export interface PageTag {
  id: number;
  name: string;
}

export interface PageVersionSummary {
  id: number;
  revision: number;
  title: string;
  createdAt: string;
  savedAt: string;
  createdByDisplayName: string | null;
  restoredFromVersionId: number | null;
  restoredFromCreatedAt: string | null;
}

export interface PageVersion {
  id: number;
  pageId: number;
  revision: number;
  title: string;
  body: string | null;
  previousBody: string | null;
  createdAt: string;
  savedAt: string;
  createdByDisplayName: string | null;
}

export interface PageLinkedTask {
  id: number;
  systemId: string;
  name: string;
  statusName: string | null;
  statusColor: string | null;
  statusCategory: StatusCategory | null;
  sprintName: string | null;
  assigneeNames: string[];
}

export interface PageLinkedPage {
  id: number;
  title: string;
  slug: string;
}

export interface PageBacklink {
  pageId: number;
  title: string;
  slug: string;
  snippet: string | null;
}

export interface PageLinks {
  tasks: PageLinkedTask[];
  pages: PageLinkedPage[];
  backlinks: PageBacklink[];
}

export interface UpdatePageRequest {
  status?: PageStatus;
  tagIds?: number[];
}

export interface ArchivedPage {
  id: number;
  kind: PageKind;
  title: string;
  slug: string;
  archivedAt: string | null;
  archivedByDisplayName: string | null;
  createdByUserId: string | null;
}

export interface CreatePageRequest {
  kind: PageKind;
  parentId?: number | null;
  title?: string | null;
  templateKey?: string | null;
  sprintId?: number | null;
  projectId?: number | null;
}

export interface RecentPage {
  id: number;
  parentId?: number | null;
  title: string;
  slug: string;
  visitedAt: string;
  updatedAt: string;
  updatedByDisplayName?: string | null;
}

export enum PageReviewReason {
  stale = 0,
  unvisited = 1,
  brokenLinks = 2,
}

export interface PageNeedingReview {
  id: number;
  parentId?: number | null;
  title: string;
  slug: string;
  updatedAt: string;
  reason: PageReviewReason;
}

export interface SavePageRequest {
  title: string;
  body: string | null;
  revision: number;
}

export interface MovePageRequest {
  parentId: number | null;
  sortOrder: number;
}

// Mirrors PageBody.MaxBytes on the server.
export const pageBodyMaxBytes = 1024 * 1024;

export type PageTransformAction =
  'summarise' | 'tighten' | 'translate' | 'continue';

export interface TransformPageTextRequest {
  pageId: number;
  action: PageTransformAction;
  text: string;
  language?: string;
}

export interface TransformPageTextResult {
  text: string;
}

export interface ImportPagesResult {
  created: number;
  skipped: string[];
  firstSlug?: string | null;
}
