import {
  computed,
  effect,
  inject,
  Injectable,
  linkedSignal,
} from '@angular/core';
import { PageTreeNode } from '@core/models/pages';
import { CurrentRouteService } from '@core/router/current-route.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { PageCommandsService } from '@core/services/page-commands.service';
import { TreeMove } from '@static/components/tree/tree.types';
import { pageTreeResource } from '../resources/pages.resource';
import { placementFor } from '../util/page-placement';

const expandedStoragePrefix = 'pages.tree.expanded.';

export const archiveNodeId = -1;
export const homeNodeId = -2;
export const archiveSegment = 'archive';

interface ExpansionSource {
  key: string;
  openId: number | null;
}

const homePattern = /^\/[^/?#]+\/pages\/?(?:[?#].*)?$/;
const pageSegmentPattern = /^\/[^/?#]+\/pages\/([^/?#]+)/;

// The workspace page tree, shared by the sidebar and the page views under one layout.
@Injectable()
export class PageTreeStore {
  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly currentRoute = inject(CurrentRouteService);
  private readonly commands = inject(PageCommandsService);
  private readonly tree = pageTreeResource();

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

  readonly footerSelectedId = computed(() => {
    return this.archiveSelected() ? archiveNodeId : null;
  });

  private readonly nodesById = computed(() => {
    return new Map(this.nodes().map((node) => [node.id, node]));
  });

  private readonly storageKey = computed(() => {
    return `${expandedStoragePrefix}${this.workspaceSlug()}`;
  });

  // Reloads from storage when the workspace changes, and always opens the way to the open page.
  readonly expanded = linkedSignal<ExpansionSource, readonly number[]>({
    source: () => ({ key: this.storageKey(), openId: this.selectedId() }),
    computation: (source, previous) => {
      const sameWorkspace = previous?.source.key === source.key;
      const current = sameWorkspace ? previous.value : readExpanded(source.key);

      return this.withAncestorsOf(source.openId, current);
    },
  });

  constructor() {
    effect(() => writeExpanded(this.storageKey(), this.expanded()));
  }

  link(slug: string): string[] {
    return ['/', this.workspaceSlug(), 'pages', slug];
  }

  homeLink(): string[] {
    return ['/', this.workspaceSlug(), 'pages'];
  }

  find(id: number | null): PageTreeNode | undefined {
    if (id === null) return undefined;

    return this.nodesById().get(id);
  }

  // Root first, ending with the page's parent.
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

  renameLocally(id: number, title: string) {
    this.tree.value.update((nodes) => {
      return nodes?.map((node) => (node.id === id ? { ...node, title } : node));
    });
  }
}

function readExpanded(key: string): number[] {
  try {
    const stored = localStorage.getItem(key);
    const parsed: unknown = stored ? JSON.parse(stored) : [];

    return Array.isArray(parsed) ? parsed.filter(Number.isInteger) : [];
  } catch {
    return [];
  }
}

function writeExpanded(key: string, expanded: readonly number[]) {
  try {
    localStorage.setItem(key, JSON.stringify(expanded));
  } catch {
    // Storage can be unavailable (private mode, quota); expansion just won't persist.
  }
}
