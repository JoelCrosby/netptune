import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { MatSnackBar } from '@angular/material';
import { AuthService } from '@app/services/auth/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  animations: [pullIn]
})
export class RegisterComponent {

  constructor(
    public authServices: AuthService,
    private router: Router,
    public snackbar: MatSnackBar) { }

  registerFromGroup = new FormGroup({
    emailFormControl: new FormControl('', [
      Validators.required,
      Validators.email,
    ]),
    password0FormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    password1FormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ])
  });

  get f() { return this.registerFromGroup.controls; }

  async register() {

    const result = await this.authServices.register(
      this.registerFromGroup.controls['emailFormControl'].value,
      this.registerFromGroup.controls['password1FormControl'].value
    );

    if (result.isSuccess) {
      this.router.navigate(['/home']);
    } else {
      this.snackbar.open(result.message, undefined, {
        duration: 3000,
      });
    }
  }

  backToLoginClicked(): void {
    this.router.navigate(['/auth/login']);
  }

}
