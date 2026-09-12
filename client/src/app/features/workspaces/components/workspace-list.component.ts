import { Component, computed, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { matchesQuery } from '@core/util/strings';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PinnedWorkspacesService } from '../services/pinned-workspaces.service';
import { WorkspaceListItemComponent } from './workspace-list-item.component';

interface WorkspaceRow {
  workspace: Workspace;
  isPinned: boolean;
}

@Component({
  selector: 'app-workspace-list',
  imports: [EmptyStateComponent, WorkspaceListItemComponent],
  template: `
    <div
      class="flex flex-col gap-2.5"
      (pointerenter)="pointerInside.set(true)"
      (pointerleave)="onPointerLeave()">
      @for (row of visibleRows(); track row.workspace.id) {
        <app-workspace-list-item
          [workspace]="row.workspace"
          [isPinned]="row.isPinned"
          (open)="onOpen(row.workspace)"
          (pinToggle)="onPinToggle(row.workspace)" />
      } @empty {
        @if (hasQuery()) {
          <app-empty-state
            compact
            outlined
            [title]="noMatchesTitle()"
            i18n-description="Advice shown when no workspace matches the filter"
            description="Check the spelling, or create a new workspace." />
        }
      }
    </div>

    @if (workspaces().length > 1 && visibleRows().length > 0) {
      <p
        class="mt-5 text-[12.5px] text-[rgba(var(--foreground-rgb),0.52)]"
        i18n="Explains how the workspace list is ordered">
        Pinned workspaces stay at the top. Everything else is ordered by recent
        activity.
      </p>
    }
  `,
})
export class WorkspaceListComponent {
  private readonly workspaceService = inject(WorkspaceService);
  private readonly router = inject(Router);
  private readonly pinned = inject(PinnedWorkspacesService);
  private readonly heldOrder = signal<number[] | null>(null);

  protected readonly pointerInside = signal(false);

  readonly query = input('');

  readonly workspaces = inject(WorkspaceListService).workspaces;

  protected readonly hasQuery = computed(() => !!this.query().trim());

  protected readonly visibleRows = computed(() => {
    const query = this.query();

    return this.rows().filter((row) => {
      return matchesQuery(row.workspace, query, ['name', 'description']);
    });
  });

  protected readonly noMatchesTitle = computed(() => {
    const query = this.query().trim();

    return $localize`:Shown when no workspace matches the filter. QUERY is what the user typed:No workspace matches “${query}:QUERY:”`;
  });

  protected readonly rows = computed<WorkspaceRow[]>(() => {
    const pinnedIds = this.pinned.pinnedIds();

    const rows = this.workspaces().map((workspace) => ({
      workspace,
      isPinned: pinnedIds.includes(workspace.id),
    }));

    const held = this.heldOrder();

    if (held) {
      return [...rows].sort(
        (left, right) =>
          heldIndex(held, left.workspace.id) -
          heldIndex(held, right.workspace.id)
      );
    }

    return [
      ...rows.filter((row) => row.isPinned),
      ...rows.filter((row) => !row.isPinned),
    ];
  });

  protected onOpen(workspace: Workspace) {
    this.workspaceService.setWorkspace(workspace.slug);
    this.router.navigate(['/', workspace.slug, 'projects']);
  }

  protected onPinToggle(workspace: Workspace) {
    if (this.pointerInside()) {
      this.heldOrder.set(this.rows().map((row) => row.workspace.id));
    }

    this.pinned.toggle(workspace.id);
  }

  protected onPointerLeave() {
    this.pointerInside.set(false);
    this.heldOrder.set(null);
  }
}

function heldIndex(held: number[], workspaceId: number): number {
  const index = held.indexOf(workspaceId);
  return index === -1 ? held.length : index;
}
