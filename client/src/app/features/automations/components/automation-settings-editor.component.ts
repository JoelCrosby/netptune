import { ChangeDetectionStrategy, Component, model } from '@angular/core';
import { CardHeaderComponent } from '@app/static/components/card/card-header.component';
import { CardSubtitleComponent } from '@app/static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@app/static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

@Component({
  selector: 'app-automation-settings-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardComponent,
    CheckboxComponent,
    FormInputComponent,
    CardHeaderComponent,
    CardTitleComponent,
    CardSubtitleComponent,
  ],
  template: `
    <app-card>
      <app-card-header>
        <app-card-title>Name</app-card-title>
        <app-card-subtitle>
          Give this automation a clear, recognizable name.
        </app-card-subtitle>
      </app-card-header>
      <div class="flex flex-col justify-baseline gap-4">
        <app-form-input name="name" [required]="true" [(value)]="name" />

        <app-checkbox [(checked)]="isEnabled"> Enabled </app-checkbox>
      </div>
    </app-card>
  `,
})
export class AutomationSettingsEditorComponent {
  readonly name = model('');
  readonly isEnabled = model(true);
}
