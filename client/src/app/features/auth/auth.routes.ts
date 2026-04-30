import { Routes } from '@angular/router';
import { loginGuard } from '@core/auth/login.guard';

import { canDeactivateLogin } from './guards/can-deactivate-login.guard';
import { authProvider } from './resolvers/auth-provider.resolver';
import { confirmEmail } from './resolvers/confirm-email.resolver';
import { registerInvite } from './resolvers/register-invite.resolver';
import { resetPassword } from './resolvers/reset-password.resolver';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    loadComponent: () =>
      import('./components/login/login.component').then(
        (m) => m.LoginComponent
      ),
    canActivate: [loginGuard],
    canDeactivate: [canDeactivateLogin],
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./components/register/register.component').then(
        (m) => m.RegisterComponent
      ),
    resolve: {
      invite: registerInvite,
    },
  },
  {
    path: 'request-password-reset',
    loadComponent: () =>
      import('./components/request-password-reset/request-password-reset.component').then(
        (m) => m.RequestPasswordResetComponent
      ),
    canActivate: [loginGuard],
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./components/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      ),
    resolve: {
      resetPassword: resetPassword,
    },
  },
  {
    path: 'confirm',
    loadComponent: () =>
      import('./views/confirm-view/confirm-view.component').then(
        (m) => m.ConfirmViewComponent
      ),
    resolve: {
      confirmEmail: confirmEmail,
    },
  },
  {
    path: 'auth-provider-login',
    loadComponent: () =>
      import('./components/auth-provider/auth-provider.component').then(
        (m) => m.AuthProviderComponent
      ),
    resolve: {
      authProviderResult: authProvider,
    },
  },
  { path: '**', redirectTo: 'login' },
];
