import { Component, computed, input } from '@angular/core';
import { colorBackgroundClass } from '@core/util/colors/colors';

@Component({
  selector: 'app-workspace-badge',
  host: {
    '[class]': 'className()',
  },
  template: `
    @if (logoUrl(); as url) {
      <img
        [src]="url"
        [alt]="letter()"
        class="h-full w-full rounded-[.12rem] object-cover" />
    } @else {
      {{ letter() }}
    }
  `,
})
export class WorkspaceBadgeComponent {
  readonly color = input<string | null | undefined>(null);
  readonly letter = input.required<string>();
  readonly logoUrl = input<string | null>(null);

  readonly className = computed(() => {
    const background = this.logoUrl()
      ? 'bg-transparent'
      : colorBackgroundClass(this.color());

    return `h-7 min-w-7 w-7 flex items-center justify-center overflow-hidden rounded-[.12rem] transition-opacity duration-200 text-white text-sm ${background}`;
  });
}
