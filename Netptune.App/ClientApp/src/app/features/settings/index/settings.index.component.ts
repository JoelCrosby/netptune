import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { SettingsState } from '@core/settings/settings.model';
import { selectSettings } from '@core/settings/settings.selectors';
import { changeTheme } from '@core/settings/settings.actions';

@Component({
  selector: 'app-settings-index',
  templateUrl: './settings.index.component.html',
  styleUrls: ['./settings.index.component.scss'],
})
export class SettingsIndexComponent implements OnInit {
  settings$: Observable<SettingsState>;

  themes = [
    { value: 'DEFAULT-THEME', label: 'Light' },
    { value: 'DARK-THEME', label: 'Dark' },
    { value: 'CORPORATE-THEME', label: 'Corporate' },
  ];

  constructor(private store: Store<SettingsState>) {}

  ngOnInit() {
    this.settings$ = this.store.select(selectSettings);
  }

  onThemeSelect({ value: theme }) {
    this.store.dispatch(changeTheme({ theme }));
  }
}
