import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { ButtonComponent } from '@static/components/button/button.component';
import * as BoardGroupActions from '@boards/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
}

@Component({
  selector: 'app-board-group-dialog',
  templateUrl: './board-group-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    ButtonComponent,
    DialogCloseDirective,
  ],
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
