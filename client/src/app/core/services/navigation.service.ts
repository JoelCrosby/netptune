import { inject, Injectable, signal } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { filter, map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class NavigationService {
  router = inject(Router);
  title = inject(Title);

  back = signal<string | null>(null);

  listen() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        map(() => {
          let route: ActivatedRoute = this.router.routerState.root;
          let routeTitle = '';

          while (route!.firstChild) {
            route = route.firstChild;
          }

          if (route.snapshot.data['title']) {
            routeTitle = route!.snapshot.data['title'];
          }

          this.back.set(route?.snapshot.data['back'] ?? null);

          return routeTitle;
        })
      )
      .subscribe((title: string) => {
        if (title) {
          this.title.setTitle(`Netptune - ${title}`);
        }
      });
  }
}
