import { inject, linkedSignal, Signal, untracked } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  ArchivedPage,
  PageChild,
  PageDetail,
  PageLinks,
  PageNeedingReview,
  PageTreeNode,
  PageVersion,
  PageVersionSummary,
  RecentPage,
} from '@core/models/pages';
import {
  permissionResource,
  requestFrom,
} from '@core/resources/permission.resource';
import { stableResource } from '@core/resources/stable.resource';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';

export const pageTreeResource = () => {
  return stableResource<PageTreeNode[]>({
    permission: PERMISSIONS.pages.read,
    request: () => ({ url: 'api/pages/tree' }),
    parse: (response) => response.payload ?? [],
    refreshOn: ['pages'],
  });
};

// Not refreshed on 'pages': a failed reload would swap the open editor for the not-found state.
// Edits made elsewhere arrive through pageUpdatesResource instead.
export const pageDetailResource = (slug: Signal<string | undefined>) => {
  return permissionResource<PageDetail | undefined>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(slug, (key) => ({ url: `api/pages/${key}` })),
    parse: (response) => response.payload,
  });
};

// Idle until 'pages' is refreshed after the page loads, so opening a page fetches it only once.
export const pageUpdatesResource = (slug: Signal<string | undefined>) => {
  const pagesVersion = inject(WorkspaceRefreshService).version('pages');
  const loadedAtVersion = linkedSignal({
    source: slug,
    computation: () => untracked(pagesVersion),
  });

  return permissionResource<PageDetail | undefined>({
    permission: PERMISSIONS.pages.read,
    request: () => {
      const key = slug();
      const version = pagesVersion();

      if (!key || version === loadedAtVersion()) return undefined;

      return { url: `api/pages/${key}` };
    },
    parse: (response) => response.payload,
  });
};

export interface PageVersionsKey {
  pageId: number;
  revision: number;
}

// Keyed on the revision too, so the list follows saves while the history tab is open.
export const pageVersionsResource = (
  key: Signal<PageVersionsKey | undefined>
) => {
  return permissionResource<PageVersionSummary[]>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(key, ({ pageId, revision }) => ({
      url: `api/pages/${pageId}/versions`,
      params: { revision },
    })),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
  });
};

export const pageVersionResource = (
  key: Signal<{ pageId: number; versionId: number } | undefined>
) => {
  return permissionResource<PageVersion | undefined>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(key, ({ pageId, versionId }) => ({
      url: `api/pages/${pageId}/versions/${versionId}`,
    })),
    parse: (response) => response.payload,
  });
};

// Keyed on the revision so links follow this page's saves, and refreshed with pages and tasks so
// backlinks and task names follow everyone else's. Stable, so chips keep their names while it reloads.
const noLinks: PageLinks = { tasks: [], pages: [], backlinks: [] };

export const pageLinksResource = (key: Signal<PageVersionsKey | undefined>) => {
  return stableResource<PageLinks>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(key, ({ pageId, revision }) => ({
      url: `api/pages/${pageId}/links`,
      params: { revision },
    })),
    parse: (response) => response.payload ?? noLinks,
    refreshOn: ['pages', 'tasks'],
  });
};

export const archivedPagesResource = () => {
  return permissionResource<ArchivedPage[]>({
    permission: PERMISSIONS.pages.read,
    request: () => ({ url: 'api/pages/archived' }),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['pages'],
  });
};

export const recentPagesResource = () => {
  return permissionResource<RecentPage[]>({
    permission: PERMISSIONS.pages.read,
    request: () => ({ url: 'api/pages/recent' }),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['pages'],
  });
};

export const pagesNeedingReviewResource = () => {
  return permissionResource<PageNeedingReview[]>({
    permission: PERMISSIONS.pages.read,
    request: () => ({ url: 'api/pages/needs-review' }),
    parse: (response) => response.payload ?? [],
    defaultValue: [],
    refreshOn: ['pages'],
  });
};

export const pageChildrenResource = (pageId: Signal<number | undefined>) => {
  return stableResource<PageChild[]>({
    permission: PERMISSIONS.pages.read,
    request: requestFrom(pageId, (id) => ({ url: `api/pages/${id}/children` })),
    parse: (response) => response.payload ?? [],
    refreshOn: ['pages'],
  });
};
