import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnInit,
} from '@angular/core';
import { DialogRef } from '@angular/cdk/dialog';
import * as actions from '@boards/store/groups/board-groups.actions';
import * as selectors from '@boards/store/groups/board-groups.selectors';
import { BoardViewGroup } from '@core/models/view-models/board-view';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { MatButton } from '@angular/material/button';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  templateUrl: './move-tasks-dialog.component.html',
  styleUrls: ['./move-tasks-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatButton,
    DialogActionsDirective,
    DialogCloseDirective,
    AsyncPipe
],
})
export class MoveTasksDialogComponent implements OnInit {
  groups$!: Observable<BoardViewGroup[]>;
  selected: number | null = null;

  constructor(
    private store: Store,
    private cd: ChangeDetectorRef,
    public dialogRef: DialogRef<MoveTasksDialogComponent>
  ) {}

  ngOnInit() {
    this.groups$ = this.store.select(selectors.selectAllBoardGroups);
  }

  onGroupClicked(groupId: number) {
    this.selected = groupId;
    this.cd.markForCheck();
  }

  onMoveTasksClicked() {
    const newGroupId = this.selected;

    if (newGroupId === null) return;

    this.store.dispatch(actions.moveSelectedTasks({ newGroupId }));

    this.dialogRef.close();
  }
}
