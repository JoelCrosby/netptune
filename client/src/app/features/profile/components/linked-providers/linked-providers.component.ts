import { Component, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectLoginProviders } from '@app/core/store/profile/profile.selectors';

@Component({
  selector: 'app-linked-providers',
  template: `
    <div class="max-w-120">
      <p class="text-foreground/60 mb-4 text-sm">
        These external accounts are linked to your profile and can be used to
        sign in.
      </p>
      @if (providers().length === 0) {
        <p class="text-foreground/40 text-sm">No external accounts linked.</p>
      } @else {
        <div class="flex flex-wrap gap-2">
          @for (provider of providers(); track provider) {
            <div
              class="border-border flex items-center gap-2 rounded-full border px-4 py-1.5 text-sm font-medium">
              @switch (providerKey(provider)) {
                @case ('github') {
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 24 24"
                    class="text-foreground h-4 w-4 fill-current"
                    aria-hidden="true">
                    <path
                      d="M12 2C6.475 2 2 6.475 2 12a9.994 9.994 0 006.838 9.488c.5.087.687-.213.687-.476 0-.237-.013-1.024-.013-1.862-2.512.463-3.162-.612-3.362-1.175-.113-.288-.6-1.175-1.025-1.413-.35-.187-.85-.65-.013-.662.788-.013 1.35.725 1.538 1.025.9 1.512 2.338 1.087 2.912.825.088-.65.35-1.087.638-1.337-2.225-.25-4.55-1.113-4.55-4.938 0-1.088.387-1.987 1.025-2.688-.1-.25-.45-1.275.1-2.65 0 0 .837-.262 2.75 1.026a9.28 9.28 0 012.5-.338c.85 0 1.7.112 2.5.337 1.912-1.3 2.75-1.024 2.75-1.024.55 1.375.2 2.4.1 2.65.637.7 1.025 1.587 1.025 2.687 0 3.838-2.337 4.688-4.562 4.938.362.312.675.912.675 1.85 0 1.337-.013 2.412-.013 2.75 0 .262.188.574.688.474A10.016 10.016 0 0022 12c0-5.525-4.475-10-10-10z" />
                  </svg>
                }
                @case ('microsoft') {
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 21 21"
                    class="h-4 w-4"
                    aria-hidden="true">
                    <rect x="1" y="1" width="9" height="9" fill="#f25022" />
                    <rect x="11" y="1" width="9" height="9" fill="#7fba00" />
                    <rect x="1" y="11" width="9" height="9" fill="#00a4ef" />
                    <rect x="11" y="11" width="9" height="9" fill="#ffb900" />
                  </svg>
                }
                @case ('google') {
                  <svg
                    xmlns="http://www.w3.org/2000/svg"
                    viewBox="0 0 18 18"
                    class="h-4 w-4"
                    aria-hidden="true">
                    <path
                      fill="#4285F4"
                      d="M17.64 9.2c0-.637-.057-1.251-.164-1.84H9v3.481h4.844c-.209 1.125-.843 2.078-1.796 2.717v2.258h2.908c1.702-1.567 2.684-3.875 2.684-6.615z" />
                    <path
                      fill="#34A853"
                      d="M9 18c2.43 0 4.467-.806 5.956-2.18l-2.908-2.259c-.806.54-1.837.86-3.048.86-2.344 0-4.328-1.584-5.036-3.711H.957v2.332C2.438 15.983 5.482 18 9 18z" />
                    <path
                      fill="#FBBC05"
                      d="M3.964 10.71c-.18-.54-.282-1.117-.282-1.71s.102-1.17.282-1.71V4.958H.957C.347 6.173 0 7.548 0 9s.348 2.827.957 4.042l3.007-2.332z" />
                    <path
                      fill="#EA4335"
                      d="M9 3.58c1.321 0 2.508.454 3.44 1.345l2.582-2.58C13.463.891 11.426 0 9 0 5.482 0 2.438 2.017.957 4.958L3.964 7.29C4.672 5.163 6.656 3.58 9 3.58z" />
                  </svg>
                }
                @default {
                  <span
                    class="bg-primary h-2.5 w-2.5 rounded-full"
                    aria-hidden="true"></span>
                }
              }
              {{ provider }}
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class LinkedProvidersComponent {
  private store = inject(Store);

  providers = this.store.selectSignal(selectLoginProviders);

  providerKey(provider: string) {
    return provider.trim().toLowerCase();
  }
}
