import {
  Component,
  ElementRef,
  computed,
  input,
  model,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LucideSearch, LucideX } from '@lucide/angular';
import { KeyboardKeyComponent } from '../keyboard-key/keyboard-key.component';
import { FormControlFieldComponent } from '../form-control/form-control-field.component';
import { FormControlInputDirective } from '../form-control/form-control.directives';

export type FilterInputAppearance = 'field' | 'bare';

@Component({
  selector: 'app-filter-input',
  host: { class: 'block' },
  imports: [
    LucideSearch,
    LucideX,
    NgTemplateOutlet,
    KeyboardKeyComponent,
    FormControlFieldComponent,
    FormControlInputDirective,
  ],
  template: `
    <ng-template #content>
      <svg lucideSearch [class]="iconClass()" aria-hidden="true"></svg>

      <input
        #input
        appFormInput
        type="text"
        autocomplete="off"
        [class]="inputClass()"
        [value]="value()"
        [attr.role]="inputRole()"
        [attr.aria-expanded]="expanded()"
        [attr.aria-controls]="controls()"
        [attr.aria-activedescendant]="activeDescendant()"
        [attr.aria-autocomplete]="inputRole() === 'combobox' ? 'list' : null"
        [attr.placeholder]="placeholder()"
        [attr.aria-label]="ariaLabel() ?? placeholder()"
        (input)="setValue($event)" />

      @if (value().length > 0) {
        <button
          type="button"
          tabindex="-1"
          class="text-muted hover:text-foreground flex h-4 w-4 shrink-0 items-center justify-center transition-colors"
          i18n-aria-label="Accessible label for the button that clears a filter"
          aria-label="Clear filter"
          (click)="clear()">
          <svg lucideX class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      } @else if (keyHint(); as hint) {
        <app-keyboard-key [class]="keyHintClass">{{ hint }}</app-keyboard-key>
      }
    </ng-template>

    @if (appearance() === 'bare') {
      <div
        class="border-border flex h-10.5 shrink-0 items-center gap-2.25 border-b px-3">
        <ng-container [ngTemplateOutlet]="content" />
      </div>
    } @else {
      <app-form-control-field density="compact" class="gap-1.5 px-2.5">
        <ng-container [ngTemplateOutlet]="content" />
      </app-form-control-field>
    }
  `,
})
export class FilterInputComponent {
  readonly value = model('');
  readonly placeholder = input<string | null>(null);
  readonly ariaLabel = input<string | null>(null);
  readonly appearance = input<FilterInputAppearance>('field');
  readonly keyHint = input<string | null>(null);
  readonly inputRole = input<string | null>(null);
  readonly controls = input<string | null>(null);
  readonly activeDescendant = input<string | null>(null);
  readonly expanded = input<boolean | null>(null);

  private readonly input =
    viewChild.required<ElementRef<HTMLInputElement>>('input');

  /** Quieter and squarer than the default key cap, to suit a menu row. */
  protected readonly keyHintClass =
    'min-w-0 shrink-0 rounded-sm border-[rgba(var(--foreground-rgb),0.14)] px-[5px] text-[10px] font-normal text-[rgba(var(--foreground-rgb),0.4)]';

  protected readonly iconClass = computed(() => {
    return this.appearance() === 'bare'
      ? 'h-3.75 w-3.75 shrink-0 text-[rgba(var(--foreground-rgb),0.45)]'
      : 'text-muted h-4 w-4 shrink-0';
  });

  protected readonly inputClass = computed(() => {
    return this.appearance() === 'bare'
      ? 'text-foreground min-w-0 flex-1 p-0 font-[inherit] text-sm leading-none! placeholder:text-[rgba(var(--foreground-rgb),0.4)]'
      : 'text-sm leading-none!';
  });

  focus() {
    this.input().nativeElement.focus();
  }

  protected setValue(event: Event) {
    this.value.set((event.target as HTMLInputElement).value);
  }

  protected clear() {
    this.value.set('');
    this.focus();
  }
}
