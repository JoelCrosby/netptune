import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
  imports: [FormSelectComponent, FormsModule, FormSelectOptionComponent],
})
export class SettingsComponent {
  private store = inject(Store);

  settings = this.store.selectSignal(selectSettings);

  themes = [
    { value: 'LIGHT-THEME', label: 'Light' },
    { value: 'DARK-THEME', label: 'Dark' },
  ];

  onThemeSelect(theme: string) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
