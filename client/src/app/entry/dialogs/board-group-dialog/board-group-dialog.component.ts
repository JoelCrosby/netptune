import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { apply, FormField, form, submit } from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import * as BoardGroupActions from '@app/core/store/groups/board-groups.actions';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { statusResource } from '@core/resources/status.resources';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

export interface BoardGroupDialogData {
  boardId: number;
  identifier: string;
  // Present when editing an existing group.
  boardGroupId?: number;
  name?: string;
  statusId?: number | null;
}

@Component({
  selector: 'app-board-group-dialog',
  imports: [
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogCloseDirective,
  ],
  template: `<app-dialog-title>{{
      isEdit ? 'Edit Group' : 'Add Group'
    }}</app-dialog-title>

    <div app-dialog-content>
      <form (submit)="onSubmit($event)">
        <app-form-input
          [formField]="groupForm.group"
          label="Group Name"
          maxLength="128">
        </app-form-input>

        @if (statuses.canRead()) {
          <app-form-select [formField]="groupForm.statusId" label="Status">
            <app-form-select-option [value]="null">
              No status
            </app-form-select-option>
            @for (status of statuses.value(); track status.id) {
              <app-form-select-option [value]="status.id">
                {{ status.name }}
              </app-form-select-option>
            }
          </app-form-select>
        }
      </form>
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Close</button>
      <button app-flat-button type="button" (click)="onSubmit($event)">
        {{ isEdit ? 'Save' : 'Add Group' }}
      </button>
    </div> `,
})
export class BoardGroupDialogComponent {
  private store = inject(Store);
  dialogRef = inject<DialogRef<BoardGroupDialogComponent>>(DialogRef);
  data = inject<BoardGroupDialogData>(DIALOG_DATA);

  statuses = statusResource();

  isEdit = this.data.boardGroupId !== undefined;

  groupFormModel = signal({
    group: this.data.name ?? '',
    statusId: this.data.statusId ?? null,
  });

  groupForm = form(this.groupFormModel, (schema) => {
    apply(
      schema.group,
      requiredTextSchema({ label: 'Group name', maxLength: 128 })
    );
  });

  onSubmit(event: Event) {
    event.preventDefault();

    submit(this.groupForm, async () => {
      const name = this.groupForm.group().value().trim();
      const statusId = this.groupForm.statusId().value();
      const boardGroupId = this.data.boardGroupId;

      if (boardGroupId !== undefined) {
        this.store.dispatch(
          BoardGroupActions.editBoardGroup.init({
            request: {
              boardGroupId,
              name,
              statusId: statusId ?? undefined,
              clearStatus: statusId === null,
            },
          })
        );
      } else {
        this.store.dispatch(
          BoardGroupActions.createBoardGroup.init({
            identifier: this.data.identifier,
            request: {
              name,
              boardId: this.data.boardId,
              statusId,
            },
          })
        );
      }

      this.dialogRef.close();
    });
  }
}
