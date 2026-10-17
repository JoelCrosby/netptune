export enum SourceControlProvider {
  gitHub = 0,
  azureDevOps = 1,
  gitLab = 2,
}

export enum ScmAccountType {
  organisation = 0,
  user = 1,
  group = 2,
  project = 3,
}

export enum ScmConnectionStatus {
  active = 0,
  suspended = 1,
  revoked = 2,
}

export type SourceControlCapability =
  | 'listRepositories'
  | 'readBranches'
  | 'readCommits'
  | 'readPullRequests'
  | 'readCheckRuns'
  | 'createBranch'
  | 'webhooks'
  | 'issueSync';

export interface ScmTerminology {
  pullRequest: string;
  pullRequests: string;
  issue: string;
  checkRuns: string;
}

export interface SourceControlProviderInfo {
  provider: SourceControlProvider;
  terminology: ScmTerminology;
  capabilities: SourceControlCapability[];
}

export interface SourceControlConnection {
  id: number;
  provider: SourceControlProvider;
  baseUrl: string | null;
  accountLogin: string;
  accountType: ScmAccountType;
  accountAvatarUrl: string | null;
  status: ScmConnectionStatus;
  capabilities: SourceControlCapability[];
  linkedRepoCount: number;
  createdAt: string;
}

export interface SourceControlRepo {
  id: number;
  connectionId: number;
  owner: string;
  name: string;
  fullName: string;
  defaultBranch: string;
  webUrl: string;
  isPrivate: boolean;
  isArchived: boolean;
  isAccessible: boolean;
  isLinked: boolean;
  linkedAt: string | null;
  syncedAt: string;
}

export enum TaskCodeLinkKind {
  branch = 0,
  commit = 1,
  pullRequest = 2,
}

export enum ScmPullRequestState {
  open = 0,
  draft = 1,
  merged = 2,
  closed = 3,
}

export enum ScmCheckState {
  pending = 0,
  success = 1,
  failure = 2,
  neutral = 3,
}

export interface ScmCheckSummary {
  state: ScmCheckState;
  total: number;
  passed: number;
  failed: number;
  pending: number;
  webUrl: string | null;
}

export interface TaskCodeLink {
  id: number;
  kind: TaskCodeLinkKind;
  externalRef: string;
  title: string;
  webUrl: string;
  authorLogin: string | null;
  pullRequestState: ScmPullRequestState | null;
  isRemoved: boolean;
  occurredAt: string;
  provider: SourceControlProvider;
  repoFullName: string;
  repoWebUrl: string;
  checks: ScmCheckSummary | null;
}

export interface SourceControlConnectStart {
  redirectUrl: string;
}

export interface ScmRepoSyncResult {
  added: number;
  refreshed: number;
  removed: number;
  markedInaccessible: number;
}

const providerSlugs: Record<SourceControlProvider, string> = {
  [SourceControlProvider.gitHub]: 'github',
  [SourceControlProvider.azureDevOps]: 'azure-devops',
  [SourceControlProvider.gitLab]: 'gitlab',
};

export const sourceControlProviderNames: Record<SourceControlProvider, string> =
  {
    [SourceControlProvider.gitHub]: 'GitHub',
    [SourceControlProvider.azureDevOps]: 'Azure DevOps',
    [SourceControlProvider.gitLab]: 'GitLab',
  };

export function sourceControlProviderSlug(
  provider: SourceControlProvider
): string {
  return providerSlugs[provider];
}

export function sourceControlProviderFromSlug(
  slug: string
): SourceControlProvider | null {
  const match = Object.entries(providerSlugs).find(
    ([, value]) => value === slug
  );

  return match ? (Number(match[0]) as SourceControlProvider) : null;
}
