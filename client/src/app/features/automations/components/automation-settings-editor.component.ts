import { Component, input, model } from '@angular/core';
import { ServiceAccount } from '@core/models/service-account';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-automation-settings-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: `
    <div class="flex flex-col justify-baseline gap-4">
      <app-form-input
        name="name"
        label="Name"
        [required]="true"
        [(value)]="name" />

      <app-form-select
        name="execution-user"
        label="Run as"
        hint="Actions use this service account's workspace permissions and appear as automation activity."
        placeholder="Choose a service account"
        [required]="true"
        [disabled]="!serviceAccounts().length"
        [(value)]="executionUserId">
        @for (account of serviceAccounts(); track account.userId) {
          <app-form-select-option [value]="account.userId">
            {{ account.name }}
          </app-form-select-option>
        }
      </app-form-select>

      @if (!serviceAccounts().length) {
        <p class="text-muted -mt-3 text-sm">
          Create an enabled service account before saving this automation.
        </p>
      }

      <div class="border-border bg-foreground/5 rounded-lg border p-4">
        <app-checkbox [(checked)]="isEnabled">
          <span class="flex flex-col">
            <span class="text-foreground text-sm font-medium">Enabled</span>
            <span class="text-muted text-sm">
              Turn this automation on so it runs automatically.
            </span>
          </span>
        </app-checkbox>
      </div>
    </div>
  `,
})
export class AutomationSettingsEditorComponent {
  readonly serviceAccounts = input<readonly ServiceAccount[]>([]);
  readonly name = model('');
  readonly isEnabled = model(true);
  readonly executionUserId = model<string | null>(null);
}
