import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { cn } from '../button/button.variants';

export interface EntryListItem {
  id: number;
  label: string;
  meta: string;
}

// A list of entries, each a label over a quieter line of detail; picking one reports its id.
@Component({
  selector: 'app-entry-list',
  host: { '[class]': 'hostClass()' },
  template: `
    <ul
      class="flex flex-col gap-0.5 text-[13px]"
      [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.id; let first = $first) {
        <li>
          <button
            type="button"
            class="hover:bg-foreground/5 flex w-full flex-col gap-0.5 rounded-lg px-2.5 py-2 text-left"
            [class.bg-foreground/3]="highlightFirst() && first"
            (click)="selected.emit(item.id)">
            <span class="font-medium">{{ item.label }}</span>
            <span class="text-foreground/50 text-xs">{{ item.meta }}</span>
          </button>
        </li>
      }
    </ul>
  `,
})
export class EntryListComponent {
  readonly items = input.required<readonly EntryListItem[]>();
  readonly ariaLabel = input<string | null>(null);
  // Marks the first entry, such as the current version at the top of a history.
  readonly highlightFirst = input(false, { transform: booleanAttribute });
  readonly class = input('');

  readonly selected = output<number>();

  protected readonly hostClass = computed(() => cn('block', this.class()));
}
