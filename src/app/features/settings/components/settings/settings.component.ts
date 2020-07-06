import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { SettingsState } from '@core/store/settings/settings.model';
import { selectSettings } from '@core/store/settings/settings.selectors';
import { changeTheme } from '@core/store/settings/settings.actions';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingsComponent implements OnInit {
  settings$: Observable<SettingsState>;

  themes = [
    { value: 'DEFAULT-THEME', label: 'Light' },
    { value: 'DARK-THEME', label: 'Dark' },
  ];

  constructor(private store: Store<SettingsState>) {}

  ngOnInit() {
    this.settings$ = this.store.select(selectSettings);
  }

  onThemeSelect({ value: theme }) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
