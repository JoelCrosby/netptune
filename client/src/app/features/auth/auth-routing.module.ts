import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { loginGuard } from '@core/auth/login.guard';
import { AuthProviderComponent } from './components/auth-provider/auth-provider.component';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { RequestPasswordResetComponent } from './components/request-password-reset/request-password-reset.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { authProvider } from './resolvers/auth-provider.resolver';
import { confirmEmail } from './resolvers/confirm-email.resolver';
import { registerInvite } from './resolvers/register-invite.resolver';
import { resetPassword } from './resolvers/reset-password.resolver';
import { ConfirmViewComponent } from './views/confirm-view/confirm-view.component';
import { canDeactivateLogin } from './guards/can-deactivate-login.guard';

const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  {
    path: 'login',
    component: LoginComponent,
    canActivate: [loginGuard],
    canDeactivate: [canDeactivateLogin],
  },
  {
    path: 'register',
    component: RegisterComponent,
    resolve: {
      invite: registerInvite,
    },
  },
  {
    path: 'request-password-reset',
    component: RequestPasswordResetComponent,
    canActivate: [loginGuard],
  },
  {
    path: 'reset-password',
    component: ResetPasswordComponent,
    resolve: {
      resetPassword: resetPassword,
    },
  },
  {
    path: 'confirm',
    component: ConfirmViewComponent,
    resolve: {
      confirmEmail: confirmEmail,
    },
  },
  {
    path: 'auth-provider-login',
    component: AuthProviderComponent,
    resolve: {
      authProviderResult: authProvider,
    },
  },
  { path: '**', redirectTo: 'login' },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class AuthRoutingModule {}
