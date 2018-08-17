import { Component, OnInit } from '@angular/core';
import { AlertService } from './services/alert/alert.service';
import { AuthService } from './services/auth/auth.service';
import { Router } from '@angular/router';
import { WorkspaceService } from './services/workspace/workspace.service';
import { TransitionService } from './services/transition/transition.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {

  constructor(
    public alertService: AlertService,
    public authServives: AuthService,
    private router: Router,
    public workspaceService: WorkspaceService,
    public transitionService: TransitionService) { }

  ngOnInit(): void {
    if (this.authServives.isTokenExpired()) {
      this.router.navigate(['/login']);
    }
  }

  getData(): void {
    // this.register();
    const token = localStorage.getItem('auth_token');
    console.log('token from local storage:' + token);
  }
}
