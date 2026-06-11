import { Component, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { selectLoginProviders } from '@app/core/store/profile/profile.selectors';

@Component({
  selector: 'app-linked-providers',
  template: `
    <div class="max-w-[480px]">
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
              <svg lucideCheckCircle class="text-primary h-4 w-4"></svg>
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
}
