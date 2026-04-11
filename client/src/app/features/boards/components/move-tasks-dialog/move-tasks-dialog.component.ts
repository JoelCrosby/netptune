import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
  signal,
} from '@angular/core';
import { moveSelectedTasks } from '@boards/store/groups/board-groups.actions';
import { selectAllBoardGroups } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  styleUrls: ['./move-tasks-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogActionsDirective,
    DialogCloseDirective,
  ],
  template: `
    <app-dialog-title>Move Tasks To Group</app-dialog-title>

    <p>Select the group you wish to move the selected tasks to</p>

    <div app-dialog-content>
      <div class="group-selection">
        @for (group of groups(); track group.id) {
          <button
            [class.selected]="selected() === group.id"
            app-stroked-button
            [class.bg-primary]="selected() === group.id"
            (click)="onGroupClicked(group.id)">
            <div class="group-item">
              <span>{{ group.name }}</span>
              <div class="pill"></div>
            </div>
          </button>
        }
      </div>
    </div>

    <div app-dialog-actions align="end">
      <button mat-stroked-button app-dialog-close>Cancel</button>
      <button
        app-flat-button
        color="primary"
        (click)="onMoveTasksClicked()"
        type="button">
        Move Tasks
      </button>
    </div>
  `,
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
