import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { SharedModule } from '@app/shared/shared.module';

// Components
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { AuthRoutingModule } from './auth-routing.module';
import { StaticModule } from '@app/static/static.module';
import { ConfirmViewComponent } from './views/confirm-view/confirm-view.component';

@NgModule({
  declarations: [LoginComponent, RegisterComponent, ConfirmViewComponent],
  imports: [CommonModule, FormsModule, ReactiveFormsModule, SharedModule, StaticModule, AuthRoutingModule],
  providers: [],
})
export class AuthModule {}
