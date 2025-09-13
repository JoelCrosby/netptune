import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import { changeTheme } from '@core/store/settings/settings.actions';
import { SettingsState } from '@core/store/settings/settings.model';
import { selectSettings } from '@core/store/settings/settings.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { AsyncPipe } from '@angular/common';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormsModule } from '@angular/forms';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormSelectComponent,
    FormsModule,
    FormSelectOptionComponent,
    AsyncPipe
],
})
export class SettingsComponent implements OnInit {
  settings$!: Observable<SettingsState>;

  themes = [
    { value: 'LIGHT-THEME', label: 'Light' },
    { value: 'DARK-THEME', label: 'Dark' },
  ];

  constructor(private store: Store) {}

  ngOnInit() {
    this.settings$ = this.store.select(selectSettings);
  }

  onThemeSelect(theme: string) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
