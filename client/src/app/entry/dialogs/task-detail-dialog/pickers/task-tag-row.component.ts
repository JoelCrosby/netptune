import { httpResource } from '@angular/common/http';
import { Component, computed, input, output, signal } from '@angular/core';
import { MAX_PAGE_SIZE } from '@core/models/pagination';
import { Tag } from '@core/models/tag';
import { reloadOnRefresh } from '@core/util/reload-on-refresh';
import { LucidePlus, LucideSearch, LucideX } from '@lucide/angular';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-tag-row',
  imports: [
    DropdownMenuComponent,
    MenuItemComponent,
    LucidePlus,
    LucideSearch,
    LucideX,
  ],
  host: { class: 'flex flex-wrap items-center gap-1.5' },
  template: `
    @for (tag of tags(); track tag) {
      <span
        class="bg-primary-selected/40 group inline-flex items-center gap-1.5 rounded-md px-2.5 font-medium"
        [class]="chipClass()">
        {{ tag }}
        @if (editable()) {
          <button
            type="button"
            class="hidden cursor-pointer opacity-70 group-hover:block hover:opacity-100 focus-visible:block"
            [attr.aria-label]="removeLabel"
            (click)="removed.emit(tag)">
            <svg lucideX class="h-3 w-3"></svg>
          </button>
        }
      </span>
    }

    @if (editable()) {
      <button
        #addButton
        type="button"
        class="border-foreground/8 text-muted hover:bg-hover hover:text-foreground inline-flex shrink-0 cursor-pointer items-center justify-center rounded-md border border-dashed transition-colors"
        [class]="addButtonClass()"
        [attr.aria-label]="addLabel"
        aria-haspopup="menu"
        (click)="menu.toggle(addButton)">
        <svg lucidePlus class="h-3.5 w-3.5"></svg>
      </button>

      <app-dropdown-menu #menu panelClass="p-1" (closed)="search.set('')">
        <div class="w-60">
          <div class="border-foreground/8 relative -mx-1 -mt-1 border-b">
            <svg
              lucideSearch
              class="text-muted pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2"></svg>
            <input
              class="w-full bg-transparent py-2.5 pr-3 pl-9 text-sm focus:outline-none"
              i18n-placeholder="Placeholder in the box for searching tags"
              placeholder="Search or create a tag"
              [value]="search()"
              (input)="onSearchInput($event)"
              (keydown.enter)="addTypedTag(menu)" />
          </div>

          <div class="max-h-64 overflow-y-auto pt-1">
            @for (tag of availableTags(); track tag) {
              <button app-menu-item (click)="addTag(tag); menu.close()">
                {{ tag }}
              </button>
            } @empty {
              @if (canCreateTyped()) {
                <button app-menu-item (click)="addTypedTag(menu)">
                  <span
                    i18n="
                      Menu item that adds a tag the user typed. TAG is the text
                      they typed
                    ">
                    Add “{{
                      search()  // i18n(ph="TAG")
                    }}”
                  </span>
                </button>
              } @else {
                <div class="text-muted flex h-9 items-center px-3 text-sm">
                  <span i18n="Shown when no tags match a search">
                    No tags found
                  </span>
                </div>
              }
            }
          </div>
        </div>
      </app-dropdown-menu>
    }
  `,
})
export class TaskTagRowComponent {
  readonly tags = input<readonly string[]>([]);
  readonly size = input<'sm' | 'md'>('sm');
  readonly editable = input(true);

  readonly added = output<string>();
  readonly removed = output<string>();

  readonly search = signal('');

  readonly addLabel = $localize`:Accessible label for the control that adds a tag to a task:Add a tag`;
  readonly removeLabel = $localize`:Accessible label for the button that removes a tag from a task:Remove tag`;

  private readonly workspaceTags = httpResource<Tag[]>(
    () => ({
      url: 'api/tags/workspace',
      params: { page: 1, pageSize: MAX_PAGE_SIZE },
    }),
    { defaultValue: [] }
  );

  readonly chipClass = computed(() => {
    return this.size() === 'md'
      ? 'h-[30px] text-[13px]'
      : 'h-[26px] text-[12px]';
  });

  readonly addButtonClass = computed(() => {
    return this.size() === 'md' ? 'h-[30px] w-[30px]' : 'h-[26px] w-[26px]';
  });

  readonly availableTags = computed(() => {
    const selected = new Set(this.tags());
    const term = this.search().trim().toLowerCase();

    return this.workspaceTags
      .value()
      .map((tag) => tag.name)
      .filter((name) => !selected.has(name))
      .filter((name) => !term || name.toLowerCase().includes(term));
  });

  readonly canCreateTyped = computed(() => {
    const term = this.search().trim();

    return term.length > 0 && !this.tags().includes(term);
  });

  constructor() {
    reloadOnRefresh(this.workspaceTags, ['tags']);
  }

  protected onSearchInput(event: Event) {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected addTypedTag(menu: DropdownMenuComponent) {
    const term = this.search().trim();

    if (!term) return;

    this.addTag(term);
    menu.close();
  }

  protected addTag(tag: string) {
    if (this.tags().includes(tag)) return;

    this.added.emit(tag);
  }
}
