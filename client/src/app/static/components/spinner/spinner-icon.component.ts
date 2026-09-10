import { Component, computed, input } from '@angular/core';
import { LucideLoaderCircle } from '@lucide/angular';
import { cn } from '../button/button.variants';

// The small in-line "working on it" mark that sits beside a label, as opposed
// to app-spinner, which is the standalone loading indicator for a whole view.
@Component({
  selector: 'app-spinner-icon',
  imports: [LucideLoaderCircle],
  host: { class: 'contents' },
  template: `
    <svg
      lucideLoaderCircle
      [class]="iconClass()"
      [attr.role]="label() ? 'img' : null"
      [attr.aria-label]="label() || null"
      [attr.aria-hidden]="label() ? null : 'true'"></svg>
  `,
})
export class SpinnerIconComponent {
  // Names what is being waited on. Without it the mark is decorative and
  // hidden from screen readers.
  readonly label = input('');
  readonly class = input('');

  protected readonly iconClass = computed(() => {
    return cn('text-primary h-4 w-4 animate-spin', this.class());
  });
}
