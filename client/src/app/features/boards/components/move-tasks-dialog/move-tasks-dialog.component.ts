import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  signal,
} from '@angular/core';
import { MatButton } from '@angular/material/button';
import { moveSelectedTasks } from '@boards/store/groups/board-groups.actions';
import { selectAllBoardGroups } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  templateUrl: './move-tasks-dialog.component.html',
  styleUrls: ['./move-tasks-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, DialogActionsDirective, DialogCloseDirective],
})
export class MoveTasksDialogComponent {
  private store = inject(Store);
  private cd = inject(ChangeDetectorRef);
  dialogRef = inject<DialogRef<MoveTasksDialogComponent>>(DialogRef);

  groups = this.store.selectSignal(selectAllBoardGroups);
  selected = signal<number | null>(null);

  onGroupClicked(groupId: number) {
    this.selected.set(groupId);
    this.cd.markForCheck();
  }

  onMoveTasksClicked() {
    const newGroupId = this.selected();

    if (newGroupId === null) return;

    this.store.dispatch(moveSelectedTasks({ newGroupId }));
    this.dialogRef.close();
  }
}
