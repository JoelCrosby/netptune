import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { ConfirmEmailResolver } from './resolvers/confirm-email.resolver';
import { LoginGuardService } from '@core/auth/login-gaurd.service';

// Components
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { ConfirmViewComponent } from './views/confirm-view/confirm-view.component';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [LoginGuardService],
  },
  {
    path: 'register',
    component: RegisterComponent,
    canActivate: [LoginGuardService],
  },
  {
    path: 'confirm',
    component: ConfirmViewComponent,
    resolve: {
      confirmEmail: ConfirmEmailResolver,
    },
  },
  { path: '**', redirectTo: 'login' },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuthRoutingModule {}
