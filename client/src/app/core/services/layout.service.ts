import { DestroyRef, inject, Injectable, linkedSignal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { MediaService, MediaSize } from '@core/services/media.service';
import { filter } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class LayoutService {
  private readonly media = inject(MediaService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isMobileView = toSignal(this.media.maxWidth(MediaSize.xs), {
    initialValue: false,
  });

  private readonly open = linkedSignal(() => !this.isMobileView());

  readonly sideMenuOpen = this.open.asReadonly();

  constructor() {
    this.closeOnNavigate();
  }

  openSideMenu() {
    this.open.set(true);
  }

  toggleSideMenu() {
    this.open.update((open) => !open);
  }

  private closeOnNavigate() {
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        if (!this.isMobileView()) return;

        this.open.set(false);
      });
  }
}
