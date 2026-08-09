import {
  effect,
  EnvironmentProviders,
  inject,
  Injectable,
  provideAppInitializer,
  signal,
} from '@angular/core';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { filter, map } from 'rxjs';

const BRAND_NAME = 'Netptune';

export function provideNavigationService(): EnvironmentProviders {
  return provideAppInitializer(() => {
    inject(NavigationService).listen();
  });
}

@Injectable({ providedIn: 'root' })
export class NavigationService {
  router = inject(Router);
  title = inject(Title);

  private readonly currentWorkspace = inject(CurrentWorkspaceService);
  private readonly pageTitle = signal<string | null>(null);

  back = signal<string | null>(null);

  constructor() {
    effect(() => this.applyTitle());
  }

  listen() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        map(() => {
          let route: ActivatedRoute = this.router.routerState.root;
          let routeTitle = '';

          while (route.firstChild) {
            route = route.firstChild;
          }

          if (route.snapshot.data['title']) {
            routeTitle = route.snapshot.data['title'];
          }

          this.back.set(route?.snapshot.data['back'] ?? null);

          return routeTitle;
        })
      )
      .subscribe((title: string) => {
        this.pageTitle.set(title || null);
      });
  }

  private applyTitle() {
    const workspace = this.currentWorkspace.workspace()?.name ?? BRAND_NAME;
    const page = this.pageTitle();

    if (!page) {
      this.title.setTitle(workspace);
      return;
    }

    this.title.setTitle(
      $localize`:Browser tab title. WORKSPACE is the name of the open workspace, PAGE is the name of the current page:${workspace}:WORKSPACE: - ${page}:PAGE:`
    );
  }
}
