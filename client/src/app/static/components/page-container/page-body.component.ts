import {
  Component,
  booleanAttribute,
  computed,
  inject,
  input,
} from '@angular/core';
import { PageContainerComponent } from './page-container.component';

@Component({
  selector: 'app-page-body',
  host: { '[class]': 'hostClass()' },
  template: `
    <div [class]="contentClass()">
      <ng-content />
    </div>
  `,
})
export class PageBodyComponent {
  readonly scroll = input(false, { transform: booleanAttribute });

  private readonly container = inject(PageContainerComponent, {
    optional: true,
  });

  // The host runs edge to edge so a scrolling page keeps its scrollbar against the window
  // rather than down the middle of a centred page.
  protected readonly hostClass = computed(() => {
    const classes = ['flex min-h-0 flex-1 flex-col'];

    if (this.scroll()) classes.push('overflow-y-auto');

    return classes.join(' ');
  });

  // Padding sits inside the centred cap so the body lines up with the header band, which
  // constrains its title row the same way.
  protected readonly contentClass = computed(() => {
    const classes = [
      'flex min-h-0 flex-1 flex-col px-8 pt-4 max-md:px-3 max-md:pt-3',
    ];

    if (this.container?.constrainListContent()) {
      classes.push('mx-auto w-full max-w-[1360px]');
    }

    return classes.join(' ');
  });
}
