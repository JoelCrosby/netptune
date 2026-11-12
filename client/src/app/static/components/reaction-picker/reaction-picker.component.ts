import {
  Component,
  ElementRef,
  input,
  output,
  viewChildren,
} from '@angular/core';
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

const horizontalSteps: Record<string, number> = {
  ArrowRight: 1,
  ArrowLeft: -1,
};

@Component({
  selector: 'app-reaction-picker',
  imports: [DropdownMenuComponent, LucideSmilePlus],
  host: { class: 'inline-flex' },
  template: `
    <button
      type="button"
      class="touch:opacity-100 focus-visible:ring-primary flex h-6 w-6 items-center justify-center rounded-full border border-dashed border-neutral-300 opacity-0 transition-opacity group-hover:opacity-100 focus:opacity-100 focus-visible:ring-2 focus-visible:outline-none dark:border-neutral-600"
      i18n-aria-label="
        Accessible label for the button that opens the emoji picker for reacting
        to a comment
      "
      aria-label="Add Reaction"
      aria-haspopup="menu"
      [attr.aria-expanded]="menu.showing()"
      (click)="menu.toggle($any($event.currentTarget))">
      <svg lucideSmilePlus class="h-3.5 w-3.5"></svg>
    </button>
    <app-dropdown-menu #menu>
      <div class="flex flex-row gap-1 p-1" (keydown)="onKeydown($event)">
        @for (value of reactionValues; track value) {
          <button
            #reaction
            type="button"
            role="menuitemcheckbox"
            class="focus-visible:ring-primary flex h-8 w-8 items-center justify-center rounded-md text-base transition-colors hover:bg-neutral-100 focus-visible:ring-2 focus-visible:outline-none dark:hover:bg-neutral-800"
            [class]="isSelected(value) ? 'bg-primary/10' : ''"
            [attr.aria-label]="value"
            [attr.aria-checked]="isSelected(value)"
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

  private readonly reactions =
    viewChildren<ElementRef<HTMLButtonElement>>('reaction');

  isSelected(value: string) {
    return this.selected().includes(value);
  }

  onKeydown(event: KeyboardEvent) {
    const step = horizontalSteps[event.key];

    if (!step) return;

    event.preventDefault();

    const buttons = this.reactions().map((ref) => ref.nativeElement);
    const current = buttons.indexOf(
      document.activeElement as HTMLButtonElement
    );
    const next = (current + step + buttons.length) % buttons.length;

    buttons[next]?.focus();
  }
}
