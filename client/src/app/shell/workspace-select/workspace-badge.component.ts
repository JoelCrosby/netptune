import { Component, computed, input } from '@angular/core';
import { colorBackgroundClass } from '@core/util/colors/colors';

@Component({
  selector: 'app-workspace-badge',
  host: {
    '[class]': 'className()',
  },
  template: `{{ letter() }}`,
})
export class WorkspaceBadgeComponent {
  readonly color = input<string | null | undefined>(null);
  readonly letter = input.required<string>();

  readonly className = computed(() => {
    return `h-7 min-w-7 w-7 flex items-center justify-center rounded-[.12rem] transition-opacity duration-200 text-white text-sm ${colorBackgroundClass(this.color())}`;
  });
}
