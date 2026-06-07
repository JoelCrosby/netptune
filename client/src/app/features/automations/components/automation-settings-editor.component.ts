import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { CardComponent } from '@static/components/card/card.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-automation-settings-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CardComponent, CheckboxComponent, FormInputComponent],
  template: `
    <app-card class="min-h-0! p-5!">
      <div class="flex flex-col justify-baseline gap-4">
        <app-form-input
          label="Name"
          name="name"
          [required]="true"
          [(value)]="name" />

        <app-checkbox [(checked)]="isEnabled"> Enabled </app-checkbox>
      </div>
    </app-card>
  `,
})
export class AutomationSettingsEditorComponent {
  readonly name = model('');
  readonly isEnabled = model(true);
}
