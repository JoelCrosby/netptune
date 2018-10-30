import { Component, AfterViewInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { Validators, FormControl, FormGroup } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '../../animations';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss'],
  animations: [pullIn]
})
export class LoginComponent implements AfterViewInit {

  public isWorking = false;

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

  async login() {
    this.isWorking = true;

    await this.authServices.login(
      this.emailFormControl.value,
      this.passwordFormControl.value);

    this.isWorking = false;
  }

  createAccountClicked(): void {
    this.router.navigate(['/register']);
  }
}
