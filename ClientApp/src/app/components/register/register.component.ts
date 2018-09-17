import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { Router } from '@angular/router';

import { trigger, style, transition, animate, query } from '@angular/animations';
import { pullIn } from '../../animations';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
  animations: [pullIn]
})
export class RegisterComponent implements OnInit {

  public state = true;

  constructor(public authServices: AuthService, private router: Router) { }

  usernameControl = new FormControl('', [
    Validators.required,
  ]);

  emailFormControl = new FormControl('', [
    Validators.required,
    Validators.email,
  ]);

  password0FormControl = new FormControl('', [
    Validators.required,
    Validators.minLength(4),
  ]);

  password1FormControl = new FormControl('', [
    Validators.required,
    Validators.minLength(4),
  ]);

  registerFromGroup = new FormGroup({

  });

  ngOnInit() {
  }

  register() {
    this.authServices.register(
      this.emailFormControl.value,
      this.password1FormControl.value,
      this.usernameControl.value);
  }

  backToLoginClicked(): void {
    this.router.navigate(['/login']);
  }

}
