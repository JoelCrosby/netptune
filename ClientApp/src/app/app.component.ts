import { Component, OnInit } from '@angular/core';
import { AlertService } from './services/alert/alert.service';
import { AuthService } from './services/auth/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  title = 'app';

  constructor(public alertService: AlertService, public authServives: AuthService) {}

  ngOnInit(): void {
    this.authServives.isTokenExpired();
  }

  getData(): void {
    // this.register();
    const token = localStorage.getItem('auth_token');
    console.log('token from local storage:' + token);
  }
}
