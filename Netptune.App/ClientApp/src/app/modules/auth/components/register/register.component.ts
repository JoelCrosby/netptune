import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '../../../../animations';
import { MatSnackBar } from '@angular/material';
import { AuthService } from '../../../../services/auth/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  animations: [pullIn]
})
export class RegisterComponent implements OnInit {

  public state = true;

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

  // convenience getter for easy access to form fields
  get f() { return this.registerFromGroup.controls; }

  ngOnInit() {
  }

  async register() {

    const result = await this.authServices.register(
      this.registerFromGroup.controls['emailFormControl'].value,
      this.registerFromGroup.controls['password1FormControl'].value
    );

    if (result.isSuccess) {
      this.router.navigate(['/home']);
    } else {
      this.snackbar.open(result.message, null, {
        duration: 3000,
      });
    }
  }

  backToLoginClicked(): void {
    this.router.navigate(['/login']);
  }

}
