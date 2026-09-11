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

  readonly isMobileView = toSignal(this.media.maxWidth(MediaSize.md), {
    initialValue: false,
  });

  readonly isCompactView = toSignal(this.media.maxWidth(MediaSize.lg), {
    initialValue: false,
  });

  private readonly open = linkedSignal<boolean, boolean>({
    source: this.isMobileView,
    computation: () => false,
  });

  private readonly pageScroll = signal(false);

  readonly sideMenuOpen = this.open.asReadonly();
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

  closeSideMenu() {
    this.open.set(false);
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
      .subscribe(() => this.open.set(false));
  }
}
