import { Component } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  email = '';
  password = '';

  constructor(public authServives: AuthService) {}

  login(loginForm: NgForm) {
    this.authServives.login(loginForm.value.mail, loginForm.value.pass);
  }

}
