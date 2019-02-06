import { Component } from '@angular/core';
import { AppLoadService } from '@app/core/app-load/app-load.service';
import { insertRemoveSidebar } from '@app/core/animations/animations';
import { TransitionService } from '@app/services/transition/transition.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [insertRemoveSidebar]
})
export class AppComponent {

  showSidebar = false;

  constructor(private appLoad: AppLoadService,
    public transitionService: TransitionService) {
    this.appLoad.sideBarVisibility.subscribe(x => this.showSidebar = x);
  }

}
