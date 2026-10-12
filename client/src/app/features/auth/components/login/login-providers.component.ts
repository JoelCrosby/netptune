import { Component } from '@angular/core';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ProviderMarkComponent } from '@static/components/provider-mark.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';

interface LoginProvider {
  kind: string;
  name: string;
  label: string;
  href: string;
}

@Component({
  selector: 'app-login-providers',
  imports: [
    ProviderMarkComponent,
    SectionHeaderComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <app-section-header
      appearance="eyebrow"
      i18n-heading="Heading above the external sign-in provider buttons"
      heading="Or continue with" />

    <div class="grid grid-cols-3 gap-2">
      @for (provider of providers; track provider.kind) {
        <button
          app-stroked-button
          color="neutral"
          block
          type="button"
          [attr.aria-label]="provider.label"
          (click)="signInWith(provider)">
          <app-provider-mark [provider]="provider.kind" [size]="15" />
          <span>{{ provider.name }}</span>
        </button>
      }
    </div>
  `,
})
export class LoginProvidersComponent {
  protected readonly providers: LoginProvider[] = [
    {
      kind: 'github',
      name: 'GitHub',
      label: $localize`:Accessible label of the button that starts sign-in via GitHub:Continue with GitHub`,
      href: '/api/auth/github-login',
    },
    {
      kind: 'google',
      name: 'Google',
      label: $localize`:Accessible label of the button that starts sign-in via Google:Continue with Google`,
      href: '/api/auth/google-login',
    },
    {
      kind: 'microsoft',
      name: 'Microsoft',
      label: $localize`:Accessible label of the button that starts sign-in via Microsoft:Continue with Microsoft`,
      href: '/api/auth/microsoft-login',
    },
  ];

  signInWith(provider: LoginProvider) {
    location.href = provider.href;
  }
}
