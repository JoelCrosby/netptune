import { Component, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideCircleAlert, LucideLink } from '@lucide/angular';
import { firstValueFrom } from 'rxjs';
import { AuthService } from '@core/auth/auth.service';
import {
  clearPendingProviderLink,
  readPendingProviderLink,
  writePendingProviderLink,
  type PendingProviderLink,
} from '@core/auth/pending-provider-link';
import { SessionService } from '@core/services/session.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { ProviderLinkDiagramComponent } from '@static/components/provider-link-diagram.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { HeaderPromptComponent } from '@static/components/header-prompt.component';
import { SupportingTextComponent } from '@static/components/supporting-text.component';
import { AuthFormPanelComponent } from '../auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../auth-shell/auth-shell.component';

@Component({
  selector: 'app-link-provider',
  imports: [
    AuthShellComponent,
    HeaderPromptComponent,
    SupportingTextComponent,
    AuthFormPanelComponent,
    CalloutComponent,
    FlatButtonComponent,
    ProviderLinkDiagramComponent,
    RouterLink,
    StrokedButtonComponent,
  ],
  template: `
    <app-auth-shell>
      @if (!isAuthenticated()) {
        <app-header-prompt shellAction>
          <a
            app-stroked-button
            color="neutral"
            size="small"
            [routerLink]="['/auth/login']"
            i18n="Link to the login form from the provider-link page">
            Sign in
          </a>
        </app-header-prompt>
      }

      <app-auth-form-panel
        i18n-eyebrow="Label above the heading of the provider-link page"
        eyebrow="Connect provider"
        [heading]="heading()"
        [icon]="linkIcon"
        [loading]="loading()">
        @if (pendingLink(); as link) {
          <app-provider-link-diagram [provider]="link.provider" />

          <p app-supporting-text>
            <ng-container
              i18n="
                Explains that the e-mail address of an external provider already
                belongs to a Netptune account. EMAIL is that address
              ">
              A Netptune account already exists for
              <strong>{{
                link.email // i18n(ph="EMAIL")
              }}</strong
              >.
            </ng-container>
            @if (isAuthenticated()) {
              <ng-container
                i18n="
                  Invites a signed-in user to link an external provider.
                  PROVIDER is the provider name
                ">
                Connect
                {{
                  link.provider // i18n(ph="PROVIDER")
                }}
                so you can use it to sign in next time.
              </ng-container>
            } @else {
              <ng-container
                i18n="
                  Asks a signed-out visitor to sign in before linking an
                  external provider. PROVIDER is the provider name
                ">
                Sign in to that account before connecting
                {{
                  link.provider  // i18n(ph="PROVIDER")
                }}.
              </ng-container>
            }
          </p>

          @if (error(); as error) {
            <app-callout color="warn" role="alert" [icon]="alertIcon">
              {{ error }}
            </app-callout>
          }

          <div class="flex flex-col gap-2">
            @if (isAuthenticated()) {
              <button
                app-flat-button
                color="primary"
                block
                type="button"
                [disabled]="loading()"
                (click)="connectProvider()">
                {{ connectLabel() }}
              </button>
            } @else {
              <a
                app-flat-button
                color="primary"
                block
                [routerLink]="['/auth/login']">
                <span
                  i18n="
                    Prompts a signed-out visitor to sign in before linking an
                    external provider. PROVIDER is the provider name
                  ">
                  Sign in to connect
                  {{
                    link.provider // i18n(ph="PROVIDER")
                  }}
                </span>
              </a>
            }

            <a
              app-stroked-button
              color="neutral"
              block
              [routerLink]="['/auth/login']">
              <span
                i18n="Link back to the login form from the provider-link page">
                Back to sign in
              </span>
            </a>
          </div>
        } @else {
          <app-callout color="warn" role="alert" [icon]="alertIcon">
            <span
              i18n="
                Shown when an external provider link has expired or was not
                found
              ">
              This provider link is missing or expired.
            </span>
          </app-callout>

          <p
            app-supporting-text
            i18n="Tells the visitor how to retry an expired provider link">
            Start the provider sign-in again from the sign-in page.
          </p>

          <a
            app-stroked-button
            color="neutral"
            block
            [routerLink]="['/auth/login']">
            <span
              i18n="Link back to the login form from the provider-link page">
              Back to sign in
            </span>
          </a>
        }
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class LinkProviderComponent {
  private route = inject(ActivatedRoute);
  private session = inject(SessionService);
  private router = inject(Router);
  private authService = inject(AuthService);
  private snackbar = inject(SnackbarService);

  protected readonly linkIcon = LucideLink;
  protected readonly alertIcon = LucideCircleAlert;

  isAuthenticated = inject(SessionService).isAuthenticated;
  pendingLink = signal<PendingProviderLink | null>(this.getPendingLink());
  loading = signal(false);
  error = signal<string | null>(null);

  heading = computed(() => {
    const provider =
      this.pendingLink()?.provider ??
      $localize`:Stands in for the external sign-in provider's name when it is unknown:provider`;

    return $localize`:Heading of the page that links an external sign-in provider to an account. PROVIDER is the provider name, e.g. GitHub:Connect ${provider}:PROVIDER:`;
  });

  connectLabel = computed(() => {
    const provider = this.pendingLink()?.provider ?? '';

    if (this.loading()) {
      return $localize`:Button that links an external provider while the request runs:Connecting…`;
    }

    return $localize`:Button that links an external provider to the signed-in account. PROVIDER is the provider name:Connect ${provider}:PROVIDER:`;
  });

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
      this.session.establish(user);
      this.snackbar.open(
        $localize`:Confirmation shown after linking an external sign-in provider. PROVIDER is the provider name:${pending.provider}:PROVIDER: connected`
      );
      await this.router.navigate(['/workspaces']);
    } catch {
      this.error.set(
        $localize`:Error shown when an external provider link cannot be used:This provider link is invalid or expired.`
      );
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
