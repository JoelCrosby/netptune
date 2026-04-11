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
      <div class="flex flex-row flex-wrap my-8 justify-between">
        @for (group of groups(); track group.id) {
          <button
            app-stroked-button
            class="mx-[7rem] w-full uppercase"
            [class.selected]="selected() === group.id"
            [class.text-foreground]="selected() === group.id"
            [class.bg-primary/25]="selected() === group.id"
            (click)="onGroupClicked(group.id)">
            <div class="flex flex-col justify-around items-center gap-[0.2rem] w-full">
              <span>{{ group.name }}</span>
              <div class="h-[0.2rem] w-full rounded-full bg-[rgba(var(--foreground-rgb),0.1)] [.selected_&]:bg-primary/40"></div>
            </div>
          </button>
        }
      </div>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Cancel</button>
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
