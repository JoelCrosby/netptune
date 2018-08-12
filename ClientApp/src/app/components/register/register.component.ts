import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { NgForm } from '@angular/forms';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

  email = '';
  username = '';

  password0 = '';
  password1 = '';

  constructor(public authServices: AuthService) { }

  ngOnInit() {
  }

  register(registerForm: NgForm) {
    this.authServices.register(registerForm.value.mail, registerForm.value.pass1, registerForm.value.username);
  }

}
