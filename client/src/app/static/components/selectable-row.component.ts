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
  'hover:bg-hover focus-visible:ring-primary flex cursor-pointer items-center gap-3 rounded p-2 transition-colors select-none focus-visible:ring-2 focus-visible:outline-none';

@Component({
  selector: 'app-selectable-row',
  imports: [CheckboxComponent],
  host: {
    role: 'checkbox',
    tabindex: '0',
    '[class]': 'hostClass()',
    '[attr.aria-checked]': 'checked()',
    '(click)': 'toggle()',
    '(keydown.space)': 'onKeydown($event)',
    '(keydown.enter)': 'onKeydown($event)',
  },
  template: `
    <app-checkbox class="pointer-events-none" [checked]="checked()" />
    <ng-content />
  `,
})
export class SelectableRowComponent {
  readonly checked = input(false, { transform: booleanAttribute });
  readonly class = input('');

  readonly toggled = output<boolean>();

  protected readonly hostClass = computed(() => cn(rowClasses, this.class()));

  protected toggle() {
    this.toggled.emit(!this.checked());
  }

  // Space would scroll the list and Enter would submit the surrounding form.
  protected onKeydown(event: Event) {
    event.preventDefault();
    this.toggle();
  }
}
