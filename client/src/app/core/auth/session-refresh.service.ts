import { Location } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  EnvironmentProviders,
  Service,
  inject,
  provideAppInitializer,
} from '@angular/core';
import { AuthService } from '@core/auth/auth.service';
import {
  hasSessionHint,
  rememberSessionHint,
  sessionHintExpiry,
} from '@core/auth/session-hint';
import { LoginResponse } from '@core/models/session';
import { SessionService } from '@core/services/session.service';
import { firstValueFrom } from 'rxjs';

const REFRESH_LOCK = 'netptune-refresh';

export function provideAuthRefresh(): EnvironmentProviders {
  return provideAppInitializer(async () => {
    const refresher = inject(SessionRefreshService);
    const session = inject(SessionService);
    const location = inject(Location);

    if (location.path().split('?')[0] === '/auth/auth-provider-login') {
      return;
    }

    if (!hasSessionHint()) {
      return;
    }

    try {
      const user = await refresher.refresh();

      session.establish(user);
    } catch (err: unknown) {
      if (isSessionRejection(err)) {
        session.clear();
      }
    }
  });
}

// Anything else (offline, a 5xx mid-deploy) says nothing about the session itself.
export function isSessionRejection(err: unknown): boolean {
  return (
    err instanceof HttpErrorResponse &&
    (err.status === 401 || err.status === 400)
  );
}

@Service()
export class SessionRefreshService {
  private readonly authService = inject(AuthService);
  private readonly session = inject(SessionService);

  private pending: Promise<LoginResponse> | null = null;

  refresh(): Promise<LoginResponse> {
    const hintBeforeLock = sessionHintExpiry();

    this.pending ??= withRefreshLock(() => {
      return this.refreshUnderLock(hintBeforeLock);
    }).finally(() => {
      this.pending = null;
    });

    return this.pending;
  }

  // Refresh tokens are single use, so tabs refreshing at once would revoke each
  // other's token. A hint that changed while this tab waited on the lock means
  // another tab just refreshed the shared cookies, so there's nothing to redo.
  private async refreshUnderLock(
    hintBeforeLock: string | null
  ): Promise<LoginResponse> {
    const user = this.session.currentUser();
    const hint = sessionHintExpiry();
    const refreshedWhileWaiting = hint !== null && hint !== hintBeforeLock;

    if (user && refreshedWhileWaiting) {
      return { ...user, expires: hint };
    }

    const refreshed = await firstValueFrom(this.authService.refresh());

    // Before the lock is released, or the next tab would not see this refresh.
    rememberSessionHint(refreshed.expires);

    return refreshed;
  }
}

function withRefreshLock<T>(work: () => Promise<T>): Promise<T> {
  const hasLocks = 'locks' in navigator;

  if (!hasLocks) {
    return work();
  }

  return navigator.locks.request(REFRESH_LOCK, work);
}
