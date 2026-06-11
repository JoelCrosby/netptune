import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { AuthService } from '@app/core/auth/auth.service';
import { Store } from '@ngrx/store';
import { loginSuccess } from '@app/core/store/auth/auth.actions';
import { selectIsAuthenticated } from '@app/core/store/auth/auth.selectors';
import { firstValueFrom } from 'rxjs';
import { SnackbarService } from '@app/static/components/snackbar/snackbar.service';
import {
  clearPendingProviderLink,
  readPendingProviderLink,
  writePendingProviderLink,
  type PendingProviderLink,
} from '@app/core/auth/pending-provider-link';

@Component({
  selector: 'app-link-provider',
  imports: [
    AuthPageContainerComponent,
    RouterLink,
    StrokedButtonComponent,
    FlatButtonComponent,
  ],
  template: `<app-auth-page-container>
    <section
      class="bg-background border-border z-1 flex w-md max-w-[calc(100vw-2rem)] flex-col gap-5 rounded border p-8 shadow-lg">
      <img
        src="assets/apple-touch-icon.png"
        alt="Netptune Logo"
        width="72"
        height="72"
        class="mx-auto my-2" />

      <div class="flex flex-col gap-2 text-center">
        <h3 class="font-normal tracking-normal">Connect {{ provider() }}</h3>
        @if (pendingLink(); as link) {
          <p class="text-muted-foreground text-sm leading-6">
            A Netptune account already exists for {{ link.email }}. Sign in to
            that account before connecting {{ link.provider }}.
          </p>
        } @else {
          <p class="text-muted-foreground text-sm leading-6">
            This provider link is missing or expired. Start the provider sign-in
            again.
          </p>
        }
      </div>

      @if (error()) {
        <div
          class="text-warn rounded-[0.4rem] bg-[rgba(var(--warn-rgb),0.06)] p-[0.6rem] text-center text-sm font-medium">
          {{ error() }}
        </div>
      }

      @if (pendingLink(); as link) {
        @if (isAuthenticated()) {
          <button
            app-flat-button
            color="primary"
            type="button"
            [disabled]="loading()"
            (click)="connectProvider()">
            Connect {{ link.provider }}
          </button>
        } @else {
          <a app-flat-button color="primary" [routerLink]="['/auth/login']">
            Sign in to connect {{ link.provider }}
          </a>
        }
      }

      <a app-stroked-button color="primary" [routerLink]="['/auth/login']">
        Back to sign in
      </a>
    </section>
  </app-auth-page-container>`,
})
export class LinkProviderComponent {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private authService = inject(AuthService);
  private store = inject(Store);
  private snackbar = inject(SnackbarService);

  isAuthenticated = this.store.selectSignal(selectIsAuthenticated);
  pendingLink = signal<PendingProviderLink | null>(this.getPendingLink());
  loading = signal(false);
  error = signal<string | null>(null);

  provider = () => this.pendingLink()?.provider ?? 'provider';

  async connectProvider() {
    const pending = this.pendingLink();

    if (!pending || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const user = await firstValueFrom(
        this.authService.linkProvider({ token: pending.token })
      );

      clearPendingProviderLink();
      this.store.dispatch(loginSuccess({ user }));
      this.snackbar.open(`${pending.provider} connected`);
      await this.router.navigate(['/workspaces']);
    } catch {
      this.error.set('This provider link is invalid or expired.');
    } finally {
      this.loading.set(false);
    }
  }

  private getPendingLink() {
    const token = this.route.snapshot.queryParamMap.get('token');
    const provider = this.route.snapshot.queryParamMap.get('provider');
    const email = this.route.snapshot.queryParamMap.get('email');

    if (token && provider && email) {
      const link = { token, provider, email };
      writePendingProviderLink(link);
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: {},
        replaceUrl: true,
      });

      return link;
    }

    return readPendingProviderLink();
  }
}
