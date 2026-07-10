import { Component, model } from '@angular/core';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-automation-settings-editor',
  imports: [CheckboxComponent, FormInputComponent],
  template: `
    <div class="flex flex-col justify-baseline gap-4">
      <app-form-input
        name="name"
        label="Name"
        [required]="true"
        [(value)]="name" />

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
  readonly name = model('');
  readonly isEnabled = model(true);
}
