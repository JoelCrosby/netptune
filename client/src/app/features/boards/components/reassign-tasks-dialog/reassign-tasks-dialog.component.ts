import { DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  inject,
} from '@angular/core';
import * as actions from '@app/core/store/groups/board-groups.actions';
import { selectBoardGroupsUsersModel } from '@app/core/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';

@Component({
  selector: 'app-reassign-tasks-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    AvatarComponent,
    DialogActionsDirective,
    DialogCloseDirective,
  ],
  template: `
    <app-dialog-title>Re-assign Tasks</app-dialog-title>

    <p>Select the user you wish to assign the selected tasks to</p>

    <div app-dialog-content>
      <div class="m-8 flex flex-row flex-wrap justify-center">
        @for (user of users(); track user) {
          <button
            app-stroked-button
            class="mx-4 w-full max-w-50"
            [class.selected]="selected === user.id"
            [class.text-foreground]="selected === user.id"
            [class.bg-primary/25]="selected === user.id"
            (click)="onUserClicked(user.id)">
            <div class="flex flex-1 flex-row items-center gap-[0.8rem]">
              <app-avatar
                class="shrink-0 basis-6"
                size="sm"
                [name]="user.displayName"
                [imageUrl]="user.pictureUrl">
              </app-avatar>
              <span>{{ user.displayName }}</span>
            </div>
          </button>
        }
      </div>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Cancel</button>
      <button
        app-flat-button
        class="bg-primary"
        (click)="onReassignTasksClicked()"
        type="button">
        Re-assign Tasks
      </button>
    </div>
  `,
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
