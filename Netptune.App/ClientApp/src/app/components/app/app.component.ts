import { Component, OnInit } from '@angular/core';
import { AlertService } from '../../services/alert/alert.service';
import { AuthService } from '../../services/auth/auth.service';
import { Router } from '@angular/router';
import { TransitionService } from '../../services/transition/transition.service';
import { LayoutService } from '../../services/layout/layout.service';
import { insertRemoveSidebar } from '../../animations';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [
    insertRemoveSidebar
  ]
})
export class AppComponent implements OnInit {

  constructor(
    public alertService: AlertService,
    public authServives: AuthService,
    private router: Router,
    public layoutService: LayoutService,
    public transitionService: TransitionService) { }

  ngOnInit(): void {

  }

  getData(): void {
    const token = localStorage.getItem('auth_token');
  }
}
