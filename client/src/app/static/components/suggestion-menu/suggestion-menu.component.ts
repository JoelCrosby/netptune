import {
  afterRenderEffect,
  Component,
  computed,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '../button/button.variants';
import {
  editorMenuIcon,
  editorMenuItem,
  editorMenuList,
} from '../editor/editor-menu.styles';

export interface SuggestionItem {
  id: string;
  label: string;
  hint?: string | null;
  icon?: LucideIconInput | null;
  group?: string | null;
}

interface SuggestionRow {
  item: SuggestionItem;
  index: number;
  groupStart: string | null;
}

@Component({
  selector: 'app-suggestion-menu',
  imports: [LucideDynamicIcon],
  host: { '[class]': 'hostClass()' },
  template: `
    <ul
      role="listbox"
      [class]="listClass"
      [attr.aria-label]="ariaLabel()"
      (mousedown)="$event.preventDefault()"
      #list>
      @for (row of rows(); track row.item.id) {
        @if (row.groupStart) {
          <li
            role="presentation"
            class="text-foreground/45 px-2 pt-1.5 pb-0.5 text-[11px] font-semibold tracking-[0.06em] uppercase">
            {{ row.groupStart }}
          </li>
        }
        <li
          role="option"
          [class]="itemClass"
          [attr.data-index]="row.index"
          [attr.data-active]="row.index === activeIndex()"
          [attr.aria-selected]="row.index === activeIndex()"
          (mouseenter)="hovered.emit(row.index)"
          (mousedown)="picked.emit(row.index)">
          @if (row.item.icon; as icon) {
            <svg [lucideIcon]="icon" [class]="iconClass"></svg>
          }
          <span class="min-w-0 flex-1 truncate">{{ row.item.label }}</span>
          @if (row.item.hint) {
            <span class="text-foreground/40 shrink-0 font-mono text-xs">{{
              row.item.hint
            }}</span>
          }
        </li>
      } @empty {
        <li class="text-foreground/50 px-2 py-1.5">{{ emptyLabel() }}</li>
      }
    </ul>
  `,
})
export class SuggestionMenuComponent {
  readonly items = input.required<readonly SuggestionItem[]>();
  readonly activeIndex = input(0);
  readonly ariaLabel = input<string | null>(null);
  readonly emptyLabel = input('');
  readonly class = input('');

  readonly picked = output<number>();
  readonly hovered = output<number>();

  private readonly list = viewChild.required<ElementRef<HTMLElement>>('list');

  protected readonly hostClass = computed(() => cn('block', this.class()));
  protected readonly listClass = cn(
    editorMenuList,
    'custom-scroll max-h-72 w-[280px] overflow-y-auto text-sm'
  );
  protected readonly itemClass = editorMenuItem;
  protected readonly iconClass = cn(editorMenuIcon, 'shrink-0');

  protected readonly rows = computed<SuggestionRow[]>(() => {
    let previousGroup: string | null = null;

    return this.items().map((item, index) => {
      const group = item.group ?? null;
      const groupStart = group !== previousGroup ? group : null;

      previousGroup = group;

      return { item, index, groupStart };
    });
  });

  constructor() {
    afterRenderEffect(() => {
      const index = this.activeIndex();
      const option = this.list().nativeElement.querySelector(
        `[data-index="${index}"]`
      );

      option?.scrollIntoView({ block: 'nearest' });
    });
  }
}
