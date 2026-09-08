import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { CheckboxComponent } from './checkbox/checkbox.component';
import { cn } from './button/button.variants';

const rowClasses =
  'focus-visible:ring-primary flex items-center gap-3 rounded p-2 transition-colors select-none focus-visible:ring-2 focus-visible:outline-none';

const enabledClasses = 'hover:bg-hover cursor-pointer';
const disabledClasses = 'cursor-not-allowed opacity-55';

@Component({
  selector: 'app-selectable-row',
  imports: [CheckboxComponent],
  host: {
    role: 'checkbox',
    '[attr.tabindex]': 'disabled() ? null : 0',
    '[class]': 'hostClass()',
    '[attr.aria-checked]': 'checked()',
    '[attr.aria-disabled]': 'disabled() || null',
    '(click)': 'toggle()',
    '(keydown.space)': 'onKeydown($event)',
    '(keydown.enter)': 'onKeydown($event)',
  },
  template: `
    <app-checkbox
      class="pointer-events-none"
      [checked]="checked()"
      [disabled]="disabled()" />
    <ng-content />
  `,
})
export class SelectableRowComponent {
  readonly checked = input(false, { transform: booleanAttribute });
  readonly disabled = input(false, { transform: booleanAttribute });
  readonly class = input('');

  readonly toggled = output<boolean>();

  protected readonly hostClass = computed(() => {
    const state = this.disabled() ? disabledClasses : enabledClasses;

    return cn(rowClasses, state, this.class());
  });

  protected toggle() {
    if (this.disabled()) return;

    this.toggled.emit(!this.checked());
  }

  // Space would scroll the list and Enter would submit the surrounding form.
  protected onKeydown(event: Event) {
    event.preventDefault();
    this.toggle();
  }
}
