import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { apply, FormField, form, submit } from '@angular/forms/signals';
import { Status } from '@core/models/status';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { AutomationRuleSummaryComponent } from '../components/automation-rule-summary.component';
import {
  AutomationAction,
  AutomationTrigger,
} from '../models/automation.models';

export interface AutomationCloneDialogData {
  ruleName: string;
  trigger: AutomationTrigger;
  actions: AutomationAction[];
  statuses: Status[];
}

export interface AutomationCloneDialogResult {
  name: string;
}

@Component({
  selector: 'app-automation-clone-dialog',
  imports: [
    AutomationRuleSummaryComponent,
    DialogTitleComponent,
    FormField,
    FormInputComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `<app-dialog-title>Clone Automation</app-dialog-title>

    <form
      app-dialog-content
      class="flex w-160 max-w-full flex-col gap-4"
      (submit)="submit($event)">
      <p class="text-muted text-sm">
        Cloning
        <span class="text-foreground font-medium">{{
          dialogData.ruleName
        }}</span
        >. The copy keeps the same trigger and actions, and starts disabled so
        you can review it before it runs.
      </p>

      <app-form-input
        [formField]="cloneForm.name"
        label="Name"
        [noMargin]="true"
        maxLength="256" />

      <app-automation-rule-summary
        [trigger]="dialogData.trigger"
        [actions]="dialogData.actions"
        [statuses]="dialogData.statuses" />
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">Cancel</button>
      <button app-flat-button type="button" (click)="submit($event)">
        Clone Automation
      </button>
    </div>`,
})
export class AutomationCloneDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<AutomationCloneDialogResult, AutomationCloneDialogComponent>
    >(DialogRef);

  readonly dialogData = inject<AutomationCloneDialogData>(DIALOG_DATA);

  readonly cloneFormModel = signal({
    name: `${this.dialogData.ruleName} (copy)`.slice(0, 256),
  });

  readonly cloneForm = form(this.cloneFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({ label: 'Name', maxLength: 256, minLength: 2 })
    );
  });

  submit(event: Event) {
    event.preventDefault();

    submit(this.cloneForm, async () => {
      const name = this.cloneForm.name().value().trim();

      this.dialogRef.close({ name });
    });
  }
}
