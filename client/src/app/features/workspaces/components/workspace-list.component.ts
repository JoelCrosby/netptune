import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Workspace } from '@core/models/workspace';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceService } from '@core/services/workspace.service';
import { PinnedWorkspacesService } from '../services/pinned-workspaces.service';
import { WorkspaceListItemComponent } from './workspace-list-item.component';

interface WorkspaceRow {
  workspace: Workspace;
  isPinned: boolean;
}

@Component({
  selector: 'app-workspace-list',
  imports: [WorkspaceListItemComponent],
  template: `
    <div
      class="flex flex-col gap-2.5"
      (pointerenter)="pointerInside.set(true)"
      (pointerleave)="onPointerLeave()">
      @for (row of rows(); track row.workspace.id) {
        <app-workspace-list-item
          [workspace]="row.workspace"
          [isPinned]="row.isPinned"
          (open)="onOpen(row.workspace)"
          (pinToggle)="onPinToggle(row.workspace)" />
      }
    </div>

    @if (workspaces().length > 1) {
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

  // Pinning re-sorts the list, which would pull the row out from under the
  // pointer that just clicked it. Hold the order until the pointer leaves.
  private readonly heldOrder = signal<number[] | null>(null);

  protected readonly pointerInside = signal(false);

  readonly workspaces = inject(WorkspaceListService).workspaces;

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

    // The list arrives ordered by recent activity; pinning only lifts rows out
    // of it, so a stable partition keeps that order inside both groups.
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
    // Keyboard toggles have no pointer to protect, so they reorder immediately.
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

  // A workspace the hold predates sorts after the rows it does know about.
  return index === -1 ? held.length : index;
}
