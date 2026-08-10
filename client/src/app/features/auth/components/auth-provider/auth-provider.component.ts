import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { AuthPageContainerComponent } from '../auth-page-container/auth-page-container.component';

@Component({
  selector: 'app-auth-provider',
  imports: [AuthPageContainerComponent, SpinnerComponent],
  template: `
    <app-auth-page-container>
      <div class="z-1 flex flex-col items-center gap-4">
        <app-spinner diameter="2.5rem" />
        <p
          class="text-muted text-sm"
          i18n="Shown while a provider sign-in completes">
          Signing you in…
        </p>
      </div>
    </app-auth-page-container>
  `,
})
export class AuthProviderComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackbar = inject(SnackbarService);

  constructor() {
    const isSignedIn = this.route.snapshot.data['authProviderResult'] === true;

    if (!isSignedIn) {
      this.snackbar.error(
        $localize`:Shown when signing in through an external provider fails:Sign-in could not be completed. Please try again.`
      );

      void this.router.navigate(['/auth/login'], { replaceUrl: true });

      return;
    }

    // Replaced rather than pushed, so going back does not return to a URL carrying the sign-in.
    void this.router.navigate(['/workspaces'], { replaceUrl: true });
  }
}
