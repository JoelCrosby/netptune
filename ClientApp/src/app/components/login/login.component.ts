import { Component, AfterViewInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { Validators, FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';

import { trigger, style, transition, animate, query } from '@angular/animations';
import { pullIn } from '../../animations';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [pullIn]
})
export class LoginComponent implements AfterViewInit {

  public state = true;

  constructor(public authServices: AuthService, private router: Router) { }

  emailFormControl = new FormControl('', [
    Validators.required,
    Validators.email,
  ]);

  passwordFormControl = new FormControl('', [
    Validators.required,
    Validators.minLength(4),
  ]);

  loginFromGroup = new FormGroup({

  });

  ngAfterViewInit() {
  }

  login() {
    this.authServices.login(
      this.emailFormControl.value,
      this.passwordFormControl.value);
  }

  createAccountClicked(): void {
    this.router.navigate(['/register']);
  }
}
