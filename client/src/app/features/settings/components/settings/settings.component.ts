import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormField, form, required } from '@angular/forms/signals';
import { changeTheme } from '@core/store/settings/settings.actions';
import { selectSettings } from '@core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-settings',
  template: `
    <h3 class="font-overpass text-[1.4rem] font-normal">User Preferences</h3>

    @if (settings(); as settings) {
      <app-form-select
        class="w-[400px]"
        label="Theme"
        placeholder="Select Theme"
        [formField]="settingsForm.theme"
        (changed)="onThemeSelect($event)"
      >
        @for (theme of themes; track theme.value) {
          <app-form-select-option [value]="theme.value">
            {{ theme.label }}
          </app-form-select-option>
        }
      </app-form-select>
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormSelectComponent, FormSelectOptionComponent, FormField],
})
export class SettingsComponent {
  private store = inject(Store);

  settings = this.store.selectSignal(selectSettings);

  settingsFormModel = signal({
    theme: this.settings().theme ?? 'light',
  });

  settingsForm = form(this.settingsFormModel, (schema) => {
    required(schema.theme);
  });

  themes = [
    { value: 'light', label: 'Light' },
    { value: 'dark', label: 'Dark' },
  ];

  onThemeSelect(theme: string) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
