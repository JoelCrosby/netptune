import {
  DestroyRef,
  inject,
  Service,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router } from '@angular/router';
import { MediaService, MediaSize } from '@core/services/media.service';
import { filter } from 'rxjs/operators';

@Service()
export class LayoutService {
  private readonly media = inject(MediaService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly isMobileView = toSignal(this.media.maxWidth(MediaSize.xs), {
    initialValue: false,
  });

  private readonly open = linkedSignal(() => !this.isMobileView());
  private readonly pageScroll = signal(false);

  readonly sideMenuOpen = this.open.asReadonly();

  // A page that scrolls inside its own body leaves the shell's main element at a fixed
  // height, so the scrollbar gutter reserved there would never be used.
  readonly pageOwnsScroll = this.pageScroll.asReadonly();

  constructor() {
    this.closeOnNavigate();
  }

  setPageOwnsScroll(ownsScroll: boolean) {
    this.pageScroll.set(ownsScroll);
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
