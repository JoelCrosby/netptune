import { Component, computed, input } from '@angular/core';
import { cva } from 'class-variance-authority';
import { cn } from '../button/button.variants';

// `action` is a row that is itself the trigger; `container` holds a control of its
// own, so it highlights without claiming the click; `static` only displays.
export type FieldRowMode = 'action' | 'container' | 'static';

export const fieldRowVariants = cva(
  'flex h-10 w-full flex-nowrap items-center rounded-[7px] px-3 text-left text-[13px] transition-colors',
  {
    variants: {
      mode: {
        action: 'hover:bg-hover cursor-pointer',
        container: 'hover:bg-hover cursor-default',
        static: 'cursor-default',
      },
    },
    defaultVariants: {
      mode: 'action',
    },
  }
);

// Picker triggers render their own <button>, so they take these as `buttonClass`.
export const fieldRowClass = fieldRowVariants({ mode: 'action' });
export const fieldLabelClass = 'text-muted w-24 shrink-0';

// A label-and-value row in a dialog's property rail.
@Component({
  selector: 'app-field-row, [app-field-row]',
  host: { '[class]': 'hostClass()' },
  template: `
    <span [class]="labelClass()">{{ label() }}</span>
    <ng-content />
  `,
})
export class FieldRowComponent {
  readonly label = input.required<string>();
  readonly mode = input<FieldRowMode>('static');
  readonly labelWidth = input('');
  readonly class = input('');

  protected readonly labelClass = computed(() => {
    return cn(fieldLabelClass, this.labelWidth());
  });

  protected readonly hostClass = computed(() => {
    return cn(fieldRowVariants({ mode: this.mode() }), this.class());
  });
}
