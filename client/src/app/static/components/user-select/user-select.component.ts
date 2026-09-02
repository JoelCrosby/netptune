import {
  afterNextRender,
  Component,
  computed,
  ElementRef,
  inject,
  Injector,
  input,
  linkedSignal,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { debounce, form, FormField } from '@angular/forms/signals';
import {
  UserSelectOption,
  UserSelectValue,
} from '@core/models/view-models/user-select-option';
import {
  userSelectResource,
  UserSelectQuery,
} from '@core/resources/user-select.resource';
import { LucideSearch } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';
import { cn } from '../button/button.variants';
import { DropdownMenuComponent } from '../dropdown-menu/dropdown-menu.component';
import { SpinnerComponent } from '../spinner/spinner.component';
import {
  UserSelectOptionComponent,
  userSelectOptionId,
} from './user-select-option.component';

@Component({
  selector: 'app-user-select',
  imports: [
    AvatarComponent,
    FormField,
    DropdownMenuComponent,
    UserSelectOptionComponent,
    SpinnerComponent,
    LucideSearch,
  ],
  template: `
    <button
      type="button"
      [class]="triggerClass()"
      aria-haspopup="listbox"
      [attr.aria-expanded]="menu.showing()"
      [class.flex-col]="compact()"
      [disabled]="disabled()"
      #origin
      (click)="toggle(origin)">
      <ng-content>
        @for (user of value(); track user.id) {
          <div
            class="flex flex-row items-center gap-1.5 rounded transition-colors">
            <app-avatar
              [imageUrl]="user.pictureUrl"
              [name]="user.displayName"
              [isServiceAccount]="user.isServiceAccount ?? false"
              size="sm" />
            @if (!compact()) {
              <small class="text-sm font-medium tracking-tight">
                {{ user.displayName }}
              </small>
            }
          </div>
        }
        @if (!value().length) {
          <span class="truncate text-sm font-medium tracking-tight">
            {{ label() }}
          </span>
        }
      </ng-content>
    </button>

    <app-dropdown-menu #menu panelRole="none" (closed)="onClosed()">
      <div
        class="flex flex-col"
        [class]="compact() ? 'w-56' : 'w-72'"
        (keydown)="handleKeyDown($event)">
        <div
          class="relative -mx-1 -mt-1 border-b border-neutral-200 dark:border-neutral-700">
          <svg
            lucideSearch
            class="text-muted pointer-events-none absolute top-1/2 left-3 h-4 w-4 -translate-y-1/2"></svg>
          <input
            #searchInput
            role="combobox"
            aria-expanded="true"
            aria-autocomplete="list"
            [attr.aria-controls]="listboxId"
            class="w-full bg-transparent py-2.5 pr-3 pl-9 text-sm focus:outline-none"
            i18n-placeholder="
              Placeholder in the box for searching workspace members
            "
            placeholder="Search.."
            [attr.aria-activedescendant]="activeDescendantId()"
            [formField]="searchForm.term" />
        </div>

        <div
          [id]="listboxId"
          role="listbox"
          [attr.aria-label]="label()"
          aria-multiselectable="true"
          class="max-h-64 overflow-y-auto pt-1">
          @for (option of options(); track option.id) {
            <app-user-select-option
              [option]="option"
              [active]="isActive(option)"
              [selected]="isSelected(option)"
              (clicked)="select($event)" />
          } @empty {
            <div class="text-muted flex h-9 items-center gap-2 px-3 text-sm">
              @if (loading()) {
                <app-spinner diameter="1rem" />
                <span i18n="Shown while workspace members are being searched">
                  Searching...
                </span>
              } @else {
                {{ noResults() }}
              }
            </div>
          }
        </div>
      </div>
    </app-dropdown-menu>
  `,
})
export class UserSelectComponent {
  readonly value = model<UserSelectValue[]>([]);
  readonly compact = input(false);
  readonly disabled = input(false);
  readonly label = input('Select Users');
  readonly noResults = input('No results found...');
  readonly excludeServiceAccounts = input(false);

  readonly buttonClass = input('');

  protected readonly triggerClass = computed(() => {
    return cn(
      'text-foreground hover:bg-hover flex w-full cursor-pointer flex-row flex-wrap items-center justify-start gap-2 rounded border-0 bg-transparent p-2 text-left text-sm transition-colors focus:outline-none disabled:cursor-default disabled:hover:bg-transparent',
      this.buttonClass()
    );
  });

  readonly selectChange = output<UserSelectOption>();
  readonly closed = output();

  readonly listboxId = `user-select-listbox-${crypto.randomUUID()}`;

  private readonly injector = inject(Injector);
  private readonly menu = viewChild.required(DropdownMenuComponent);
  private readonly searchInput =
    viewChild<ElementRef<HTMLInputElement>>('searchInput');
  private readonly hasOpened = signal(false);

  readonly searchFormModel = signal({ term: '' });
  readonly searchForm = form(this.searchFormModel, (schema) => {
    debounce(schema.term, 300);
  });

  private readonly query = computed<UserSelectQuery>(() => ({
    search: this.searchForm.term().value(),
    enabled: this.hasOpened(),
    excludeServiceAccounts: this.excludeServiceAccounts(),
  }));

  private readonly usersResource = userSelectResource(this.query);

  readonly options = computed(
    () => this.usersResource.value()?.payload?.items ?? []
  );

  readonly loading = this.usersResource.isLoading;

  readonly activeIndex = linkedSignal({
    source: this.options,
    computation: () => 0,
  });

  private readonly valueIdSet = computed(
    () => new Set(this.value().map((user) => user.id))
  );

  private readonly activeOption = computed(
    () => this.options()[this.activeIndex()] ?? null
  );

  readonly activeDescendantId = computed(() => {
    const active = this.activeOption();

    return active ? userSelectOptionId(active.id) : null;
  });

  toggle(origin: HTMLElement) {
    if (this.menu().showing()) {
      this.close();
    } else {
      this.open(origin);
    }
  }

  open(origin: HTMLElement) {
    this.hasOpened.set(true);
    this.menu().open(origin);
    this.focusSearchInput();
  }

  close() {
    this.menu().close();
  }

  onClosed() {
    this.searchFormModel.set({ term: '' });
    this.closed.emit();
  }

  select(option: UserSelectOption) {
    this.selectChange.emit(option);
  }

  isActive(option: UserSelectOption) {
    return option.id === this.activeOption()?.id;
  }

  isSelected(option: UserSelectOption) {
    return this.valueIdSet().has(option.id);
  }

  handleKeyDown(event: KeyboardEvent) {
    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.moveActiveIndex(1);
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.moveActiveIndex(-1);
        break;
      case 'Home':
        event.preventDefault();
        this.setActiveIndex(0);
        break;
      case 'End':
        event.preventDefault();
        this.setActiveIndex(this.options().length - 1);
        break;
      case 'Enter':
        event.preventDefault();
        this.selectActiveOption();
        break;
      case 'Escape':
        event.preventDefault();
        this.close();
        break;
    }
  }

  private selectActiveOption() {
    const active = this.activeOption();

    if (!active) return;

    this.select(active);
  }

  private moveActiveIndex(delta: number) {
    const count = this.options().length;

    if (!count) return;

    this.setActiveIndex((this.activeIndex() + delta + count) % count);
  }

  private setActiveIndex(index: number) {
    const maxIndex = this.options().length - 1;

    if (maxIndex < 0) return;

    this.activeIndex.set(Math.max(0, Math.min(index, maxIndex)));
    this.scrollActiveOptionIntoView();
  }

  private focusSearchInput() {
    afterNextRender(() => this.searchInput()?.nativeElement.focus(), {
      injector: this.injector,
    });
  }

  private scrollActiveOptionIntoView() {
    const active = this.activeOption();

    if (!active) return;

    queueMicrotask(() => {
      const element = document.getElementById(userSelectOptionId(active.id));

      element?.scrollIntoView({ block: 'nearest' });
    });
  }
}
