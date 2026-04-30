import { Routes } from '@angular/router';
import { SettingsViewComponent } from './views/settings-view/settings-view.component';

export const routes: Routes = [
  { path: '**', component: SettingsViewComponent },
];
