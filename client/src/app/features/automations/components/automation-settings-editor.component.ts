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

      <app-checkbox [(checked)]="isEnabled"> Enabled </app-checkbox>
    </div>
  `,
})
export class AutomationSettingsEditorComponent {
  readonly name = model('');
  readonly isEnabled = model(true);
}
