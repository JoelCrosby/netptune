import {
  booleanAttribute,
  Component,
  computed,
  input,
  numberAttribute,
} from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-side-panel',
  host: {
    '[class]': 'hostClass()',
    '[style.width.px]': 'collapsed() ? 0 : width()',
    '[inert]': 'collapsed()',
    '[attr.aria-hidden]': 'collapsed() || null',
  },
  template: `
    <div class="h-full" [style.width.px]="width()">
      <ng-content />
    </div>
  `,
})
export class SidePanelComponent {
  readonly collapsed = input(false, { transform: booleanAttribute });
  readonly width = input(300, { transform: numberAttribute });
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      'block shrink-0 overflow-hidden transition-[width] duration-[180ms] ease-out motion-reduce:transition-none',
      this.class()
    );
  });
}
