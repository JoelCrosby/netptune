import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { Field, form, required } from '@angular/forms/signals';
import { changeTheme } from '@core/store/settings/settings.actions';
import { selectSettings } from '@core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormSelectComponent, FormSelectOptionComponent, Field],
})
export class SettingsComponent {
  private store = inject(Store);

  settings = this.store.selectSignal(selectSettings);
  settingsControl = new FormControl(this.settings().theme);

  settingsFormModel = signal({
    theme: this.settings().theme ?? 'LIGHT-THEME',
  });

  settingsForm = form(this.settingsFormModel, (schema) => {
    required(schema.theme);
  });

  themes = [
    { value: 'LIGHT-THEME', label: 'Light' },
    { value: 'DARK-THEME', label: 'Dark' },
  ];

  onThemeSelect(theme: string) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
