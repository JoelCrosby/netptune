import { Location } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { NavigationService } from '@app/core/services/navigation.service';
import { LucideArrowLeft } from '@lucide/angular';

@Component({
  selector: 'app-page-header-back-link',
  template: `
    @if (show()) {
      <button
        type="button"
        class="text-foreground/70 hover:text-foreground focus-visible:ring-primary inline-flex cursor-pointer items-center rounded text-sm font-medium tracking-[0.225px] transition focus-visible:ring-2 focus-visible:outline-none"
        (click)="location.back()">
        <svg
          lucideArrowLeft
          class="mr-[0.4rem] h-4 w-4"
          aria-hidden="true"></svg>
        <span> {{ show() || 'Go back' }} </span>
      </button>
    }
  `,
  imports: [LucideArrowLeft],
})
export class PageHeaderBackLinkComponent {
  location = inject(Location);
  navigation = inject(NavigationService);

  show = computed(() => {
    return this.navigation.back();
  });
}
