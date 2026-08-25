import {
  EnvironmentProviders,
  inject,
  provideAppInitializer,
} from '@angular/core';
import {
  Event as RouterEvent,
  NavigationCancel,
  NavigationCancellationCode,
  NavigationEnd,
  NavigationError,
  Router,
} from '@angular/router';
import { filter, take } from 'rxjs/operators';

const loaderId = 'app-loader';
const dismissedClass = 'app-loader-dismissed';
const fadeMs = 200;

export function provideAppLoader(): EnvironmentProviders {
  return provideAppInitializer(() => {
    const router = inject(Router);

    router.events.pipe(filter(isNavigationSettled), take(1)).subscribe(dismiss);
  });
}

function isNavigationSettled(event: RouterEvent): boolean {
  const rendered =
    event instanceof NavigationEnd || event instanceof NavigationError;

  if (rendered) return true;

  if (!(event instanceof NavigationCancel)) return false;

  const willNavigateAgain =
    event.code === NavigationCancellationCode.Redirect ||
    event.code === NavigationCancellationCode.SupersededByNewNavigation;

  return !willNavigateAgain;
}

function dismiss(): void {
  const loader = document.getElementById(loaderId);

  if (!loader) return;

  loader.classList.add(dismissedClass);

  setTimeout(() => loader.remove(), fadeMs);
}
