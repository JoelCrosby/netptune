import { Component } from '@angular/core';
import { AppLoadService } from '../../core/app-load.service';
import { insertRemoveSidebar } from '../../core/animations';
import { TransitionService } from '../../services/transition/transition.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [insertRemoveSidebar]
})
export class AppComponent {

  showSidebar = false;

  constructor(private appLoad: AppLoadService,
    public transitionService: AppLoadService) {
    this.appLoad.sideBarVisibility.subscribe(x => this.showSidebar = x);
  }

}
