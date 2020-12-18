import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginGuardService } from '@core/auth/login-gaurd.service';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { RequestPasswordResetComponent } from './components/request-password-reset/request-password-reset.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { ConfirmEmailResolver } from './resolvers/confirm-email.resolver';
import { RegisterInviteResolver } from './resolvers/register-invite.resolver';
import { ResetPasswordResolver } from './resolvers/reset-password.resolver';
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
    resolve: {
      invite: RegisterInviteResolver,
    },
  },
  {
    path: 'request-password-reset',
    component: RequestPasswordResetComponent,
    canActivate: [LoginGuardService],
  },
  {
    path: 'reset-password',
    component: ResetPasswordComponent,
    resolve: {
      resetPassword: ResetPasswordResolver,
    },
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
