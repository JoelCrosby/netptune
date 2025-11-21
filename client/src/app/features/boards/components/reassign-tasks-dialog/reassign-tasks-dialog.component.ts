import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
} from '@angular/core';
import { MatButton } from '@angular/material/button';
import * as actions from '@boards/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  selector: 'app-reassign-tasks-dialog',
  templateUrl: './reassign-tasks-dialog.component.html',
  styleUrls: ['./reassign-tasks-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    AvatarComponent,
    DialogActionsDirective,
    DialogCloseDirective,
  ],
})
export class ReassignTasksDialogComponent {
  private store = inject(Store);
  private cd = inject(ChangeDetectorRef);
  dialogRef = inject<DialogRef<ReassignTasksDialogComponent>>(DialogRef);

  users = this.store.selectSignal(selectBoardGroupsUsersModel);

  selected: string | null = null;

  onUserClicked(userId: string) {
    this.selected = userId;
    this.cd.markForCheck();
  }

  onReassignTasksClicked() {
    if (!this.selected) {
      return;
    }

    const assigneeId = this.selected;
    this.store.dispatch(actions.reassignTasks({ assigneeId }));
  }
}
