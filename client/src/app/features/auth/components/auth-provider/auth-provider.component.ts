import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { routeAfterSignIn } from '@core/auth/route-after-sign-in';
import { LucideLogIn } from '@lucide/angular';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SpinnerStatusComponent } from '@static/components/spinner/spinner-status.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';

@Component({
  selector: 'app-auth-provider',
  imports: [AuthShellComponent, AuthFormPanelComponent, SpinnerStatusComponent],
  template: `
    <app-auth-shell>
      <app-auth-form-panel
        i18n-eyebrow="Label above the heading of the sign-in form"
        eyebrow="Netptune account"
        i18n-heading="Heading shown while a provider sign-in completes"
        heading="Signing you in"
        [icon]="signInIcon">
        <app-spinner-status i18n="Shown while a provider sign-in completes">
          Completing sign-in with your provider…
        </app-spinner-status>

        <p
          panelFootnote
          i18n="Footer note on the page that completes a provider sign-in">
          If this fails you will be returned to the sign-in page.
        </p>
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class AuthProviderComponent {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackbar = inject(SnackbarService);

  protected readonly signInIcon = LucideLogIn;

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
    void this.router.navigate(routeAfterSignIn(), { replaceUrl: true });
  }
}
