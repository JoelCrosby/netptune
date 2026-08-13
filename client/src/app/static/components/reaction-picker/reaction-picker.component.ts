import { Component, input, output } from '@angular/core';
import { LucideSmilePlus } from '@lucide/angular';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';

// Mirrors Netptune.Core.Constants.ReactionValues — the server rejects anything outside this set.
export const REACTION_VALUES = [
  '👍',
  '👎',
  '😄',
  '🎉',
  '😕',
  '❤️',
  '🚀',
  '👀',
] as const;

@Component({
  selector: 'app-reaction-picker',
  imports: [DropdownMenuComponent, LucideSmilePlus],
  host: { class: 'inline-flex' },
  template: `
    <button
      type="button"
      class="flex h-6 w-6 items-center justify-center rounded-full border border-dashed border-neutral-300 opacity-0 transition-opacity group-hover:opacity-100 focus:opacity-100 dark:border-neutral-600"
      i18n-aria-label="
        Accessible label for the button that opens the emoji picker for reacting
        to a comment
      "
      aria-label="Add Reaction"
      (click)="menu.toggle($any($event.currentTarget))">
      <svg lucideSmilePlus class="h-3.5 w-3.5"></svg>
    </button>
    <app-dropdown-menu #menu panelRole="listbox">
      <div class="flex flex-row gap-1 p-1">
        @for (value of reactionValues; track value) {
          <button
            type="button"
            class="flex h-8 w-8 items-center justify-center rounded-md text-base transition-colors hover:bg-neutral-100 dark:hover:bg-neutral-800"
            [class]="isSelected(value) ? 'bg-primary/10' : ''"
            [attr.aria-label]="value"
            [attr.aria-selected]="isSelected(value)"
            (click)="reactionSelect.emit(value); menu.close()">
            {{ value }}
          </button>
        }
      </div>
    </app-dropdown-menu>
  `,
})
export class ReactionPickerComponent {
  readonly selected = input<readonly string[]>([]);

  readonly reactionSelect = output<string>();

  readonly reactionValues = REACTION_VALUES;

  isSelected(value: string) {
    return this.selected().includes(value);
  }
}
