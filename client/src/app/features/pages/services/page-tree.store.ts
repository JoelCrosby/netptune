import {
  computed,
  effect,
  inject,
  Injectable,
  linkedSignal,
} from '@angular/core';
import { PageTreeNode } from '@core/models/pages';
import { BrowserSettingsService } from '@core/settings/browser-settings.service';
import { CurrentRouteService } from '@core/router/current-route.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { projectResource } from '@core/resources/project.resource';
import { PageCommandsService } from '@core/services/page-commands.service';
import { TreeMove } from '@static/components/tree/tree.types';
import { pageTreeResource } from '../resources/pages.resource';
import { placementFor } from '../util/page-placement';

export const archiveNodeId = -1;
export const homeNodeId = -2;
export const archiveSegment = 'archive';

interface ExpansionSource {
  key: string;
  openId: number | null;
}

const homePattern = /^\/[^/?#]+\/pages\/?(?:[?#].*)?$/;
const spaceParam = 'space';
const pageSegmentPattern = /^\/[^/?#]+\/pages\/([^/?#]+)/;

export interface PageSpace {
  project: ProjectViewModel;
  pageCount: number;
}

@Injectable()
export class PageTreeStore {
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly currentRoute = inject(CurrentRouteService);
  private readonly commands = inject(PageCommandsService);
  private readonly settings = inject(BrowserSettingsService);
  private readonly tree = pageTreeResource();
  private readonly projects = projectResource();

  readonly workspaceSlug = computed(() => this.currentWorkspace.slug() ?? '');
  readonly nodes = computed(() => this.tree.value() ?? []);
  readonly isLoading = computed(() => this.tree.value() === undefined);

  private readonly openSegment = computed(() => {
    const match = pageSegmentPattern.exec(this.currentRoute.url());

    return match ? decodeURIComponent(match[1]) : null;
  });

  readonly homeSelected = computed(() => {
    return homePattern.test(this.currentRoute.url());
  });

  readonly archiveSelected = computed(
    () => this.openSegment() === archiveSegment
  );

  readonly selectedSlug = computed(() => {
    return this.archiveSelected() ? null : this.openSegment();
  });

  readonly selectedId = computed(() => {
    const slug = this.selectedSlug();
    const selected = this.nodes().find((node) => node.slug === slug);

    return selected?.id ?? null;
  });

  private readonly requestedSpaceKey = computed(() => {
    if (!this.homeSelected()) return null;

    const query = this.currentRoute.url().split('?')[1] ?? '';

    return new URLSearchParams(query).get(spaceParam);
  });

  readonly activeSpace = computed<ProjectViewModel | null>(() => {
    const selectedId = this.selectedId();
    const projects = this.projects.value();

    if (selectedId !== null) {
      const selected = this.find(selectedId);
      const projectId = selected ? this.spaceIdOf(selected) : null;

      return projects.find((project) => project.id === projectId) ?? null;
    }

    const key = this.requestedSpaceKey();

    return projects.find((project) => project.key === key) ?? null;
  });

  readonly activeSpaceId = computed(() => this.activeSpace()?.id ?? null);

  readonly spaceNodes = computed(() => {
    const spaceId = this.activeSpaceId();

    return this.nodes().filter((node) => this.spaceIdOf(node) === spaceId);
  });

  readonly spaces = computed<PageSpace[]>(() => {
    const counts = new Map<number, number>();

    for (const node of this.nodes()) {
      if (node.projectId == null) continue;

      counts.set(node.projectId, (counts.get(node.projectId) ?? 0) + 1);
    }

    return this.projects.value().map((project) => {
      return { project, pageCount: counts.get(project.id) ?? 0 };
    });
  });

  private readonly knownProjectIds = computed(() => {
    return new Set(this.projects.value().map((project) => project.id));
  });

  readonly footerSelectedId = computed(() => {
    return this.archiveSelected() ? archiveNodeId : null;
  });

  private readonly nodesById = computed(() => {
    return new Map(this.nodes().map((node) => [node.id, node]));
  });

  readonly expanded = linkedSignal<ExpansionSource, readonly number[]>({
    source: () => ({ key: this.workspaceSlug(), openId: this.selectedId() }),
    computation: (source, previous) => {
      const sameWorkspace = previous?.source.key === source.key;
      const current = sameWorkspace
        ? previous.value
        : this.readExpanded(source.key);

      return this.withAncestorsOf(source.openId, current);
    },
  });

  constructor() {
    effect(() => this.writeExpanded(this.workspaceSlug(), this.expanded()));
  }

  link(slug: string): string[] {
    return ['/', this.workspaceSlug(), 'pages', slug];
  }

  homeLink(): string[] {
    return ['/', this.workspaceSlug(), 'pages'];
  }

  spaceQuery(project: ProjectViewModel | null): Record<string, string> | null {
    return project ? { [spaceParam]: project.key } : null;
  }

  // A page whose project was deleted, or that the user cannot see, falls back to the workspace's own pages.
  private spaceIdOf(node: PageTreeNode): number | null {
    const projectId = node.projectId ?? null;
    const isVisibleProject =
      projectId !== null &&
      (this.projects.isLoading() || this.knownProjectIds().has(projectId));

    return isVisibleProject ? projectId : null;
  }

  find(id: number | null): PageTreeNode | undefined {
    if (id === null) return undefined;

    return this.nodesById().get(id);
  }

  ancestorsOf(id: number): PageTreeNode[] {
    const ancestors: PageTreeNode[] = [];
    const visited = new Set<number>();
    let parent = this.find(this.find(id)?.parentId ?? null);

    while (parent && !visited.has(parent.id)) {
      visited.add(parent.id);
      ancestors.unshift(parent);
      parent = this.find(parent.parentId);
    }

    return ancestors;
  }

  private withAncestorsOf(
    id: number | null,
    expanded: readonly number[]
  ): readonly number[] {
    if (id === null) return expanded;

    const ancestorIds = this.ancestorsOf(id).map((node) => node.id);
    const missing = ancestorIds.filter(
      (ancestorId) => !expanded.includes(ancestorId)
    );

    return missing.length ? [...expanded, ...missing] : expanded;
  }

  expand(id: number) {
    const alreadyExpanded = this.expanded().includes(id);

    if (alreadyExpanded) return;

    this.expanded.update((current) => [...current, id]);
  }

  move(move: TreeMove) {
    const target = this.find(move.targetId);

    if (!target) return;

    const placement = placementFor(
      this.nodes(),
      move.nodeId,
      target,
      move.position
    );

    this.tree.value.update((nodes) => {
      return nodes?.map((node) => {
        return node.id === move.nodeId ? { ...node, ...placement } : node;
      });
    });

    this.commands.move(move.nodeId, placement);
  }

  renameLocally(id: number, title: string, slug: string) {
    this.tree.value.update((nodes) => {
      return nodes?.map((node) => {
        return node.id === id ? { ...node, title, slug } : node;
      });
    });
  }

  private readExpanded(workspace: string): number[] {
    const stored = this.settings.get('pages.treeExpanded')?.[workspace];

    return Array.isArray(stored) ? stored.filter(Number.isInteger) : [];
  }

  private writeExpanded(workspace: string, expanded: readonly number[]) {
    this.settings.update('pages.treeExpanded', (byWorkspace) => {
      return { ...byWorkspace, [workspace]: [...expanded] };
    });
  }
}
