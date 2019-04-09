import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { ActionSettingsChangeTheme } from '../store/settings.actions';
import { selectSettings } from '../store/settings.selectors';
import { SettingsState } from '../store/settings.model';

@Component({
  selector: 'app-settings-index',
  templateUrl: './settings.index.component.html',
  styleUrls: ['./settings.index.component.scss'],
})
export class SettingsIndexComponent implements OnInit {
  settings$: Observable<SettingsState>;

  themes = [{ value: 'DEFAULT-THEME', label: 'Light' }, { value: 'DARK-THEME', label: 'Dark' }];

  constructor(private store: Store<SettingsState>) {}

  ngOnInit() {
    this.settings$ = this.store.select(selectSettings);
  }

  onThemeSelect({ value: theme }) {
    this.store.dispatch(new ActionSettingsChangeTheme({ theme }));
  }
}
