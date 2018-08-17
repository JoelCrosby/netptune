import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../services/auth/auth.service';
import { TransitionService } from '../../services/transition/transition.service';
import {NgbTooltipConfig} from '@ng-bootstrap/ng-bootstrap';

@Component({
  selector: 'app-side-bar',
  templateUrl: './side-bar.component.html',
  styleUrls: ['./side-bar.component.scss']
})
export class SideBarComponent implements OnInit {

  public sidebarStyle = 'open';

  constructor(public authService: AuthService, public transitionService: TransitionService, config: NgbTooltipConfig) {
    config.placement = 'right';
    config.container = 'body';
  }

  ngOnInit() {
  }

}
