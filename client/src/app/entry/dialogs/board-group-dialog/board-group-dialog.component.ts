import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
}

@Component({
  selector: 'app-board-group-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogCloseDirective,
  ],
  template: `<app-dialog-title>Add Group</app-dialog-title>

    <div app-dialog-content>
      <form (submit)="onSubmit($event)">
        <app-form-input
          [formField]="groupForm.group"
          label="Group Name"
          maxLength="128">
        </app-form-input>
      </form>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Close</button>
      <button app-flat-button (click)="onSubmit($event)">Add Group</button>
    </div> `,
})
export class BoardGroupDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<BoardGroupDialogComponent>>(DialogRef);
  data = inject<BoardGroupDialogData>(DIALOG_DATA);

  groupFormModel = signal({
    group: '',
  });

  groupForm = form(this.groupFormModel, (schema) => {
    required(schema.group);
  });

  onSubmit(event: Event) {
    event.preventDefault();

    if (this.groupForm().invalid()) return;

    const name = this.groupForm.group().value();
    const identifier = this.data.identifier;

    this.store.dispatch(
      BoardGroupActions.createBoardGroup({
        identifier,
        request: {
          name,
          boardId: this.data.boardId,
        },
      })
    );

    this.dialogRef.close();
  }
}
