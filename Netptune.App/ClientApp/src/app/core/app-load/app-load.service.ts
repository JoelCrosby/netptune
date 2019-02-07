import { Injectable, Injector } from '@angular/core';
import { NavigationStart, RouteConfigLoadEnd, RouteConfigLoadStart, Router } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { filter } from 'rxjs/operators';

@Injectable()
export class AppLoadService {

  subscriptions = new Subscription();

  sidebarRoutes = [
    '/projects',
    '/users',
    '/tasks',
    '/profile',
    '/dashboard',
    '/projects',
    '/projects'
  ];

  sideBarVisibility = new Subject<boolean>();
  loadingRouteConfig = new Subject<boolean>();

  constructor(private injector: Injector) { }

  initializeApp(): Promise<any> {

    const router = this.injector.get(Router);

    return new Promise((resolve, reject) => {

      this.subscriptions.add(
        router.events
          .pipe(filter(event => event instanceof NavigationStart))
          .subscribe((val: NavigationStart) => {
            const showSideBar = this.sidebarRoutes.includes(val.url);
            this.sideBarVisibility.next(showSideBar);
          })
      );

      this.subscriptions.add(
        router.events.subscribe(event => {
          if (event instanceof RouteConfigLoadStart) {
            this.loadingRouteConfig.next(true);
          } else if (event instanceof RouteConfigLoadEnd) {
            this.loadingRouteConfig.next(false);
          }
        })
      );

      resolve();
    });
  }

}
