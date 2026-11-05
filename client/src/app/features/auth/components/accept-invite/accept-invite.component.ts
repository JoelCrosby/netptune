import { Component, inject } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideCircleAlert, LucideMail } from '@lucide/angular';
import { AuthService } from '@core/auth/auth.service';
import {
  clearPendingWorkspaceInvite,
  readPendingWorkspaceInvite,
  writePendingWorkspaceInvite,
} from '@core/auth/pending-workspace-invite';
import { SessionService } from '@core/services/session.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { mutation } from '@core/util/mutation';
import { requireSuccess } from '@core/util/rxjs-operators';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SpinnerStatusComponent } from '@static/components/spinner/spinner-status.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';

@Component({
  selector: 'app-accept-invite',
  imports: [
    AuthShellComponent,
    AuthFormPanelComponent,
    CalloutComponent,
    FlatButtonComponent,
    RouterLink,
    SpinnerStatusComponent,
    StrokedButtonComponent,
    SupportingTextComponent,
  ],
  template: `
    <app-auth-shell>
      <app-auth-form-panel
        i18n-eyebrow="
          Label above the heading of the page that accepts a workspace
          invitation
        "
        eyebrow="Workspace invitation"
        i18n-heading="Heading of the page that accepts a workspace invitation"
        heading="Join workspace"
        [icon]="inviteIcon"
        [loading]="accepting()">
        @if (error(); as error) {
          <app-callout color="warn" role="alert" [icon]="alertIcon">
            {{ error }}
          </app-callout>

          <a
            app-stroked-button
            color="neutral"
            block
            [routerLink]="['/workspaces']">
            <span i18n="Link from the invitation page to the workspace list">
              Go to your workspaces
            </span>
          </a>
        } @else if (!code) {
          <app-callout color="warn" role="alert" [icon]="alertIcon">
            <span
              i18n="
                Shown when the invitation page is opened without an invitation
                code
              ">
              This invitation link is missing or expired.
            </span>
          </app-callout>

          <a
            app-stroked-button
            color="neutral"
            block
            [routerLink]="['/auth/login']">
            <span i18n="Link back to the login form from the invitation page">
              Back to sign in
            </span>
          </a>
        } @else if (!isAuthenticated()) {
          <p
            app-supporting-text
            i18n="
              Asks a signed-out visitor to sign in before accepting a workspace
              invitation
            ">
            Sign in with the invited email address to join the workspace.
          </p>

          <a
            app-flat-button
            color="primary"
            block
            [routerLink]="['/auth/login']">
            <span
              i18n="
                Button that sends a signed-out visitor to sign in before
                accepting a workspace invitation
              ">
              Sign in to accept
            </span>
          </a>
        } @else {
          <app-spinner-status
            i18n="Shown while a workspace invitation is being accepted">
            Joining the workspace…
          </app-spinner-status>
        }
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class AcceptInviteComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly workspaceList = inject(WorkspaceListService);
  private readonly snackbar = inject(SnackbarService);
  private readonly acceptance = mutation({ fallbackError: ACCEPT_FAILED });

  protected readonly inviteIcon = LucideMail;
  protected readonly alertIcon = LucideCircleAlert;

  protected readonly isAuthenticated = inject(SessionService).isAuthenticated;
  protected readonly code = this.readCode();
  protected readonly accepting = this.acceptance.pending;
  protected readonly error = this.acceptance.error;

  constructor() {
    if (this.code && this.isAuthenticated()) {
      this.accept(this.code);
    }
  }

  private accept(code: string) {
    const request = this.authService
      .acceptWorkspaceInvite(code)
      .pipe(requireSuccess());

    this.acceptance.run(request, {
      onSuccess: (response) => {
        clearPendingWorkspaceInvite();

        const workspaceSlug = response.payload?.workspaceSlug;

        if (!workspaceSlug) {
          this.acceptance.setError(ACCEPT_FAILED);

          return;
        }

        this.workspaceList.reload();
        this.snackbar.open(
          $localize`:Confirmation shown after a workspace invitation is accepted:You joined the workspace`
        );
        void this.router.navigate(['/', workspaceSlug], { replaceUrl: true });
      },
      onError: () => clearPendingWorkspaceInvite(),
    });
  }

  // Kept for the session, so the invite survives a detour through sign-in.
  private readCode() {
    const code = this.route.snapshot.queryParamMap.get('code');

    if (!code) {
      return readPendingWorkspaceInvite();
    }

    writePendingWorkspaceInvite(code);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });

    return code;
  }
}

const ACCEPT_FAILED = $localize`:Error shown when a workspace invitation could not be accepted and the server gave no reason:The invitation could not be accepted. Please try again.`;
