import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { LucideMailCheck } from '@lucide/angular';
import { AuthCodeRequest } from '@core/models/session';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { AuthFormPanelComponent } from '../../components/auth-form-panel/auth-form-panel.component';
import { AuthShellComponent } from '../../components/auth-shell/auth-shell.component';

@Component({
  selector: 'app-confirm-view',
  imports: [AuthShellComponent, AuthFormPanelComponent, SpinnerComponent],
  template: `
    <app-auth-shell>
      <app-auth-form-panel
        i18n-eyebrow="Label above the heading of the email confirmation page"
        eyebrow="Email verification"
        i18n-heading="Heading of the page that confirms an email address"
        heading="Confirming your email"
        [icon]="mailCheckIcon">
        <div class="flex items-center gap-3.5 py-1" role="status">
          <app-spinner diameter="22px" />
          <span
            class="text-foreground/70 min-w-0 flex-1 text-sm"
            i18n="Shown while an email verification link is checked">
            Checking your verification link…
          </span>
        </div>

        <p panelFootnote i18n="Footer note on the email confirmation page">
          You will be taken to your workspaces once this finishes.
        </p>
      </app-auth-form-panel>
    </app-auth-shell>
  `,
})
export class ConfirmViewComponent {
  private activatedRoute = inject(ActivatedRoute);
  private auth = inject(AuthCommandsService);

  protected readonly mailCheckIcon = LucideMailCheck;

  routeData = toSignal(this.activatedRoute.data);

  constructor() {
    const data = this.routeData();
    const request = data?.confirmEmail as AuthCodeRequest;

    if (!request) return;

    this.auth.confirmEmail(request);
  }
}
