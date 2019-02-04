import { Injectable, Injector } from '@angular/core';
import { NavigationStart, Router } from '@angular/router';
import { Subscription, Subject } from 'rxjs';
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

      resolve();
    });
  }

}
