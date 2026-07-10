import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { moveSelectedTasks } from '@app/core/store/groups/board-groups.actions';
import { selectAllBoardGroups } from '@app/core/store/groups/board-groups.selectors';
import { DialogContentComponent } from '@app/static/components/dialog-content/dialog-content.component';
import { statusResource } from '@core/resources/status.resources';
import { Store } from '@ngrx/store';
import { BoardGroupStatusDotComponent } from '@boards/components/board-group-status-dot/board-group-status-dot.component';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    BoardGroupStatusDotComponent,
    BadgeComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogActionsDirective,
    DialogCloseDirective,
  ],
  template: `
    @let status = statusMap();

    <app-dialog-title>Move Tasks To Group</app-dialog-title>

    <app-dialog-content>
      <p class="text-foreground/60 mb-4 text-sm">
        Select the group you wish to move the selected tasks to.
      </p>

      <div
        class="custom-scroll flex max-h-[50vh] flex-col gap-2 overflow-y-auto">
        @for (group of groups(); track group.id) {
          <button
            type="button"
            class="flex w-full cursor-pointer items-center gap-3 rounded-md border px-3 py-2.5 text-left transition-colors"
            [class]="
              selected() === group.id
                ? 'border-primary bg-primary/10 text-foreground'
                : 'border-border text-foreground/80 hover:bg-foreground/5'
            "
            (click)="onGroupClicked(group.id)">
            <span
              class="flex h-4 w-4 flex-none items-center justify-center rounded-full border-2 transition-colors"
              [class]="
                selected() === group.id
                  ? 'border-primary'
                  : 'border-foreground/30'
              ">
              @if (selected() === group.id) {
                <span class="bg-primary h-2 w-2 rounded-full"></span>
              }
            </span>

            @if (group.statusId && status.get(group.statusId); as status) {
              <app-board-group-status-dot [status]="status" />
            }

            <span class="flex-1 truncate text-sm font-medium">{{
              group.name
            }}</span>

            <span appBadge color="neutral">{{ group.tasks.length }}</span>
          </button>
        } @empty {
          <p class="text-foreground/50 py-6 text-center text-sm">
            No groups available.
          </p>
        }
      </div>
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Cancel</button>
      <button
        app-flat-button
        color="primary"
        type="button"
        [disabled]="selected() === null"
        (click)="onMoveTasksClicked()">
        Move Tasks
      </button>
    </div>
  `,
})
export class MoveTasksDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<MoveTasksDialogComponent>>(DialogRef);

  groups = this.store.selectSignal(selectAllBoardGroups);
  selected = signal<number | null>(null);

  private statuses = statusResource();

  statusMap = computed(() => {
    if (!this.statuses.hasValue()) {
      return new Map();
    }

    return new Map(this.statuses.value().map((status) => [status.id, status]));
  });

  onGroupClicked(groupId: number) {
    this.selected.set(groupId);
  }

  onMoveTasksClicked() {
    const newGroupId = this.selected();

    if (newGroupId === null) return;

    this.store.dispatch(moveSelectedTasks.init({ newGroupId }));
    this.dialogRef.close();
  }
}
