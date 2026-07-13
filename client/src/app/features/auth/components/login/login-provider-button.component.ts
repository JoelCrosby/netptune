import { Component, computed, input } from '@angular/core';

type LoginProvider = 'github' | 'google' | 'microsoft';

const providerClasses: Record<LoginProvider, string> = {
  github: 'border-0 bg-[#161b22] text-white hover:bg-[#1d232c]',
  google: 'border border-[#dadce0] bg-white text-[#3c4043] hover:bg-[#f8f9fa]',
  microsoft: 'bg-[#2f2f2f] text-white hover:bg-[#3a3a3a]',
};

@Component({
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'button[app-login-provider-button]',
  template: '<ng-content />',
  host: {
    type: 'button',
    '[class]': 'className()',
  },
})
export class LoginProviderButtonComponent {
  readonly provider = input.required<LoginProvider>();

  protected readonly className = computed(
    () =>
      `mt-0 flex w-full cursor-pointer appearance-none items-center justify-center rounded-sm px-[1.6rem] py-[0.8rem] text-center font-[inherit] text-sm font-medium whitespace-nowrap no-underline transition-colors duration-[140ms] ease-in outline-none ${providerClasses[this.provider()]}`
  );
}
