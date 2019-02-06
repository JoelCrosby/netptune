import { Component, OnInit } from '@angular/core';
import { TransitionService } from '@app/services/transition/transition.service';
import { Router, NavigationEnd } from '@angular/router';
import { WorkspaceService } from '@app/services/workspace/workspace.service';
import { AuthService } from '@app/services/auth/auth.service';

@Component({
  selector: 'app-side-bar',
  templateUrl: './side-bar.component.html',
  styleUrls: ['./side-bar.component.scss']
})
export class SideBarComponent implements OnInit {

  public sidebarStyle = 'open';

  public currentUrl: string;

  constructor(
    public authService: AuthService,
    public transitionService: TransitionService,
    public worskspaceService: WorkspaceService,
    private router: Router) {

    this.router.events.subscribe((_: NavigationEnd) => this.currentUrl = _.url);
  }

  ngOnInit() {
  }

}
