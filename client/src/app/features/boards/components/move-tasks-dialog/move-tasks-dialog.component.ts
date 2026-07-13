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
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  imports: [
    DialogTitleComponent,
    SelectableCardComponent,
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
          <app-selectable-card
            groupName="task-destination-group"
            [accessibleLabel]="'Move tasks to ' + group.name"
            [selected]="selected() === group.id"
            (selectionChange)="onGroupClicked(group.id)">
            @if (group.statusId && status.get(group.statusId); as status) {
              <app-board-group-status-dot [status]="status" />
            }

            <span class="flex-1 truncate text-sm font-medium">{{
              group.name
            }}</span>

            <app-badge color="neutral">{{ group.tasks.length }}</app-badge>
          </app-selectable-card>
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
