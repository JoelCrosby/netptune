import { Component, computed, input } from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-keyboard-key',
  host: { class: 'contents' },
  template: `
    <kbd [class]="hostClass()">
      <ng-content />
    </kbd>
  `,
})
export class KeyboardKeyComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => {
    return cn(
      // A shortcut hint means nothing without a keyboard, so it is hidden on touch.
      'border-border text-muted font-avatar touch:hidden inline-flex min-w-5 items-center justify-center rounded border px-1.5 py-0.5 text-[11px] font-medium',
      this.class(),
      // Pinned after the caller's classes: tailwind-merge treats a text-* size as
      // resetting line-height, so a caller that sets one would otherwise leave the
      // cap inheriting the surrounding line-height and standing taller than its glyph.
      'leading-none'
    );
  });
}
