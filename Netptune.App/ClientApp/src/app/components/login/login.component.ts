import { Component } from '@angular/core';
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
export class LoginComponent {

  public isWorking = false;

  public loginFromGroup = new FormGroup({
    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email,
    ]),
    passwordFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ])
  });

  // convenience getter for easy access to form fields
  get f() { return this.loginFromGroup.controls; }

  constructor(public authServices: AuthService, private router: Router) { }

  async login() {
    this.isWorking = true;

    await this.authServices.login(
      this.loginFromGroup.controls['emailFormControl'].value,
      this.loginFromGroup.controls['passwordFormControl'].value
    );

    this.isWorking = false;
  }

  onCreateAccountClicked(): void {
    this.router.navigate(['/register']);
  }
}
