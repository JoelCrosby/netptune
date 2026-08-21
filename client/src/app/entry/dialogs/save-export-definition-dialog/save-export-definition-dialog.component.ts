import { DialogRef } from '@angular/cdk/dialog';
import { Component, inject, signal } from '@angular/core';
import { apply, FormField, form, submit } from '@angular/forms/signals';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SettingRowComponent } from '@static/components/setting-row/setting-row.component';
import { SwitchComponent } from '@static/components/switch/switch.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

export interface SaveExportDefinitionDialogResult {
  name: string;
  isShared: boolean;
}

@Component({
  selector: 'app-save-export-definition-dialog',
  imports: [
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    SettingRowComponent,
    StrokedButtonComponent,
    SwitchComponent,
  ],
  template: `
    <app-dialog-title i18n="Title of the save-export-definition dialog">
      Save Export
    </app-dialog-title>

    <form app-dialog-content (submit)="submit($event)">
      <app-form-input
        [formField]="definitionForm.name"
        i18n-label="Label of the saved export name field"
        label="Name"
        i18n-hint="Explains what a saved export name is for"
        hint="You will see this name when starting an export."
        maxLength="128" />

      @if (canShare()) {
        <app-setting-row
          class="border-border rounded border px-4! py-3!"
          i18n-label="Label of the option that shares a saved export"
          label="Share with the workspace"
          i18n-hint="Explains what sharing a saved export does"
          hint="Everyone who can export will be able to use it.">
          <app-switch
            [checked]="isShared()"
            i18n-ariaLabel="
              Accessible label for the option that shares a saved export
            "
            ariaLabel="Share with the workspace"
            (changed)="isShared.set($event)" />
        </app-setting-row>
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button app-flat-button type="button" (click)="submit($event)">
        <span i18n="Button that stores the current export setup for reuse">
          Save Export
        </span>
      </button>
    </div>
  `,
})
export class SaveExportDefinitionDialogComponent {
  private readonly dialogRef =
    inject<
      DialogRef<
        SaveExportDefinitionDialogResult,
        SaveExportDefinitionDialogComponent
      >
    >(DialogRef);

  protected readonly canShare = hasPermission(
    PERMISSIONS.data.manageDefinitions
  );

  readonly isShared = signal(this.canShare());

  readonly definitionFormModel = signal({
    name: '',
  });

  readonly definitionForm = form(this.definitionFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 128,
        minLength: 2,
      })
    );
  });

  submit(event: Event) {
    event.preventDefault();

    submit(this.definitionForm, async () => {
      const name = this.definitionForm.name().value().trim();

      this.dialogRef.close({ name, isShared: this.isShared() });
    });
  }
}
