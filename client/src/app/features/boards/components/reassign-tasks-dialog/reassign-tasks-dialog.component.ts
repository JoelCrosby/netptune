import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from '@angular/core';
import { Observable } from 'rxjs';
import { AppUser } from '@core/models/appuser';
import { Selected } from '@core/models/selected';
import { selectBoardGroupsUsersModel } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { DialogRef } from '@angular/cdk/dialog';
import * as actions from '@boards/store/groups/board-groups.actions';

@Component({
    selector: 'app-reassign-tasks-dialog',
    templateUrl: './reassign-tasks-dialog.component.html',
    styleUrls: ['./reassign-tasks-dialog.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class ReassignTasksDialogComponent implements OnInit {
  users$!: Observable<Selected<AppUser>[]>;

  selected: string | null = null;

  constructor(
    private store: Store,
    private cd: ChangeDetectorRef,
    public dialogRef: DialogRef<ReassignTasksDialogComponent>
  ) {}

  ngOnInit() {
    this.users$ = this.store.select(selectBoardGroupsUsersModel);
  }

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
