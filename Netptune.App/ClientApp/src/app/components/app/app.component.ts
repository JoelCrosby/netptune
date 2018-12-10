import { Component } from '@angular/core';
import { AlertService } from '../../services/alert/alert.service';
import { TransitionService } from '../../services/transition/transition.service';
import { LayoutService } from '../../services/layout/layout.service';
import { insertRemoveSidebar } from '../../animations';
import { AuthService } from '../../services/auth/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [
    insertRemoveSidebar
  ]
})
export class AppComponent {

  constructor(
    public alertService: AlertService,
    public authServives: AuthService,
    public layoutService: LayoutService,
    public transitionService: TransitionService) { }

}
