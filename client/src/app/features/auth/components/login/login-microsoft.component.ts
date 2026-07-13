import { Component } from '@angular/core';
import { LoginProviderButtonComponent } from './login-provider-button.component';

@Component({
  selector: 'app-login-microsoft',
  imports: [LoginProviderButtonComponent],
  template: `
    <button
      app-login-provider-button
      provider="microsoft"
      (click)="onMicrosoftSignInClicked()">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        width="20"
        height="20"
        viewBox="0 0 21 21"
        role="img"
        aria-labelledby="microsoft-icon-title"
        class="mr-4">
        <title id="microsoft-icon-title">microsoft</title>
        <rect x="1" y="1" width="9" height="9" fill="#f25022" />
        <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
        <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
        <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
      </svg>

      Continue with Microsoft
    </button>
  `,
})
export class LoginMicrosoftComponent {
  onMicrosoftSignInClicked() {
    location.href = '/api/auth/microsoft-login';
  }
}
