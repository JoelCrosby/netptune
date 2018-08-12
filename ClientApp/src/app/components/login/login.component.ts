import { Component } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { NgForm } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {

  email = '';
  password = '';

  constructor(public authServices: AuthService, private router: Router) { }

  login(loginForm: NgForm) {
    this.authServices.login(loginForm.value.mail, loginForm.value.pass);
  }

  createAccountClicked(): void {
    this.router.navigate(['/register']);
  }
}
