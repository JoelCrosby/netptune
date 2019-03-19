import { Component } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { pullIn } from '@app/core/animations/animations';
import { MatSnackBar } from '@angular/material';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  animations: [pullIn],
})
export class RegisterComponent {
  constructor(private router: Router, public snackbar: MatSnackBar) {}

  registerFromGroup = new FormGroup({
    emailFormControl: new FormControl('', [Validators.required, Validators.email]),
    password0FormControl: new FormControl('', [Validators.required, Validators.minLength(4)]),
    password1FormControl: new FormControl('', [Validators.required, Validators.minLength(4)]),
  });

  get f() {
    return this.registerFromGroup.controls;
  }

  async register() {}

  backToLoginClicked(): void {
    this.router.navigate(['/auth/login']);
  }
}
