import { Routes } from '@angular/router';
import { ProfileViewComponent } from './views/profile-view/profile-view.component';

export const routes: Routes = [{ path: '**', component: ProfileViewComponent }];
