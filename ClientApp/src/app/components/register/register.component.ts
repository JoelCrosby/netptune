import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { NgForm } from '../../../../node_modules/@angular/forms';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

  constructor(public authServices: AuthService) { }

  ngOnInit() {
  }

  regiser(regiserForm: NgForm) {
    this.authServices.register(regiserForm.value.mail, regiserForm.value.pass);
  }

}
