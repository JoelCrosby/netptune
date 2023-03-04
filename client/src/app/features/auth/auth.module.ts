import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '@shared/shared.module';

// Components
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { AuthRoutingModule } from './auth-routing.module';
import { StaticModule } from '@static/static.module';
import { ConfirmViewComponent } from './views/confirm-view/confirm-view.component';
import { RequestPasswordResetComponent } from './components/request-password-reset/request-password-reset.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { AuthProviderComponent } from './components/auth-provider/auth-provider.component';

@NgModule({
  declarations: [
    LoginComponent,
    RegisterComponent,
    ConfirmViewComponent,
    RequestPasswordResetComponent,
    ResetPasswordComponent,
    AuthProviderComponent,
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    SharedModule,
    StaticModule,
    AuthRoutingModule,
  ],
})
export class AuthModule {}
