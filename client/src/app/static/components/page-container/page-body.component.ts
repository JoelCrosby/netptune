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
  template: '<ng-content />',
})
export class PageBodyComponent {
  readonly scroll = input(false, { transform: booleanAttribute });

  private readonly container = inject(PageContainerComponent, {
    optional: true,
  });

  protected readonly hostClass = computed(() => {
    const classes = [
      'flex min-h-0 flex-1 flex-col px-8 pt-4 max-[600px]:px-3 max-[600px]:pt-3',
    ];

    if (this.container?.constrainListContent()) {
      classes.push('mx-auto w-full max-w-[1360px]');
    }

    if (this.scroll()) classes.push('overflow-y-auto');

    return classes.join(' ');
  });
}
