import { Component } from '@angular/core';
import { ActivatedRoute, Router, NavigationStart } from '@angular/router';
import { Subscription } from 'rxjs';
import { insertRemoveSidebar } from '../../animations';
import { TransitionService } from '../../services/transition/transition.service';
import { filter } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  animations: [
    insertRemoveSidebar
  ]
})
export class AppComponent {

  subscriptions = new Subscription();
  showSidebar = false;

  sidebarRoutes = [
    '/projects',
    '/users',
    '/tasks',
    '/profile',
    '/dashboard',
    '/projects',
    '/projects'
  ]

  constructor(
    public transitionService: TransitionService,
    private router: Router) {

    this.subscriptions.add(
      this.router.events
        .pipe(filter(event => event instanceof NavigationStart))
        .subscribe((val: NavigationStart) => {
          console.info(val.url);
          if (this.sidebarRoutes.includes(val.url)) {
            this.showSidebar = true;
          } else {
            this.showSidebar = false;
          }
        })
    );

  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

}
