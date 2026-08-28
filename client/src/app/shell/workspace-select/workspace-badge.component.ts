import { Component, computed, input } from '@angular/core';
import { colorBackgroundClass } from '@core/util/colors/colors';

export type WorkspaceBadgeSize = 'sm' | 'md';

@Component({
  selector: 'app-workspace-badge',
  host: {
    '[class]': 'className()',
  },
  template: `
    @if (logoUrl(); as url) {
      <img [src]="url" [alt]="letter()" class="h-full w-full object-cover" />
    } @else {
      {{ letter() }}
    }
  `,
})
export class WorkspaceBadgeComponent {
  readonly color = input<string | null | undefined>(null);
  readonly letter = input.required<string>();
  readonly logoUrl = input<string | null>(null);
  readonly size = input<WorkspaceBadgeSize>('md');

  readonly className = computed(() => {
    const background = this.logoUrl()
      ? 'bg-transparent'
      : colorBackgroundClass(this.color());

    const size =
      this.size() === 'sm'
        ? 'h-5.5 w-5.5 min-w-5.5 rounded-[4px] text-xs'
        : 'h-7 w-7 min-w-7 rounded-[.12rem] text-sm';

    return `flex items-center justify-center overflow-hidden transition-opacity duration-200 text-white ${size} ${background}`;
  });
}
