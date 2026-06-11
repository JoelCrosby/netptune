import { Component } from '@angular/core';

@Component({
  selector: 'app-login-microsoft',
  template: `
    <button
      class="mt-0 flex w-full cursor-pointer appearance-none items-center justify-center rounded-sm border border-[#8c8c8c] bg-[#2f2f2f] px-[1.6rem] py-[0.8rem] text-center font-[inherit] text-sm font-medium whitespace-nowrap text-white no-underline transition-colors duration-[140ms] ease-in outline-none hover:bg-[#3a3a3a]"
      (click)="$event.preventDefault(); onMicrosoftSignInClicked()">
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
