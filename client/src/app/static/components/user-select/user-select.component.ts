import { CdkPortal } from '@angular/cdk/portal';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { debounce, form, FormField } from '@angular/forms/signals';
import { AppUser } from '@core/models/appuser';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { filterObjectArray } from '@core/util/arrays';
import { AutofocusDirective } from '../../directives/autofocus.directive';
import { AvatarComponent } from '../avatar/avatar.component';
import { UserSelectOptionComponent } from './user-select-option.component';

type User = AssigneeViewModel | AppUser;

@Component({
  selector: 'app-user-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    AvatarComponent,
    AutofocusDirective,
    FormField,
    CdkPortal,
    UserSelectOptionComponent,
  ],
  styles: [
    `
      @keyframes menu-in {
        from {
          opacity: 0;
          transform: scale(0.95) translateY(-4px);
        }
        to {
          opacity: 1;
          transform: scale(1) translateY(0);
        }
      }

      .menu-panel {
        animation: menu-in 120ms ease-out;
        transform-origin: top;
      }
    `,
  ],
  template: `
    <button
      class="text-foreground hover:bg-hover flex w-full cursor-pointer flex-row flex-wrap items-center justify-start gap-2 rounded border-0 bg-transparent p-2 text-left text-sm transition-colors focus:outline-none"
      [class.flex-col]="compact()"
      #origin
      (click)="toggle(origin)">
      @for (user of value(); track user.id) {
        <div
          class="flex flex-row items-center gap-1.5 rounded transition-colors">
          <app-avatar
            [imageUrl]="user.pictureUrl"
            [name]="user.displayName"
            size="sm" />
          @if (!compact()) {
            <small class="text-sm font-medium tracking-tight">{{
              user.displayName
            }}</small>
          }
        </div>
      }
      @if (!value().length) {
        <span class="truncate text-sm font-medium tracking-tight">{{
          label()
        }}</span>
      }
    </button>

    <ng-template cdkPortal>
      <div
        class="menu-panel bg-card border-border flex flex-col overflow-hidden rounded border shadow-lg">
        @if (options()?.length) {
          <input
            appAutofocus
            class="border-border bg-secondary text-foreground sticky top-0 m-2 rounded border px-2 py-1.5 text-sm focus:outline-none"
            placeholder="Search.."
            [formField]="searchForm.term"
            (click)="$event.stopPropagation()" />
        }
        <div class="max-h-54 overflow-y-auto p-1">
          @for (option of filteredOptions(); track option.id) {
            <app-user-select-option
              [option]="option"
              [active]="isActive(option)"
              [selected]="isSelected(option)"
              (clicked)="select($event)" />
          } @empty {
            <div
              class="text-muted-foreground flex h-9 items-center px-2 text-sm">
              {{ noResults() }}
            </div>
          }
        </div>
      </div>
    </ng-template>
  `,
})
export class UserSelectComponent {
  private overlay = inject(Overlay);
  private readonly portal = viewChild.required(CdkPortal);

  readonly options = input<AppUser[] | null>([]);
  readonly value = model<User[]>([]);
  readonly compact = input(false);
  readonly label = input('Select Users');
  readonly noResults = input('No results found...');

  readonly selectChange = output<AppUser>();
  readonly closed = output();

  isOpen = signal(false);
  selected = signal<AppUser | null>(null);
  filteredOptions = signal<AppUser[]>(this.options() ?? []);

  valueIdSet = computed(() => new Set<string>(this.value().map((v) => v.id)));

  searchFormModel = signal({ term: '' });
  searchForm = form(this.searchFormModel, (schema) => {
    debounce(schema.term, 300);
  });

  private overlayRef?: OverlayRef;

  constructor() {
    effect(() => {
      const term = this.searchForm.term().value();
      untracked(() => this.search(term));
    });
  }

  toggle(origin: HTMLElement) {
    if (this.overlayRef?.hasAttached()) {
      this.close();
    } else {
      this.open(origin);
    }
  }

  open(origin: HTMLElement) {
    this.isOpen.set(true);

    this.overlayRef = this.overlay.create({
      positionStrategy: this.overlay
        .position()
        .flexibleConnectedTo(origin)
        .withPush(false)
        .withPositions([
          {
            originX: 'start',
            originY: 'bottom',
            overlayX: 'start',
            overlayY: 'top',
            offsetY: 4,
          },
          {
            originX: 'start',
            originY: 'top',
            overlayX: 'start',
            overlayY: 'bottom',
            offsetY: -4,
          },
        ]),
      width: this.compact() ? '200px' : `${origin.offsetWidth}px`,
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-transparent-backdrop',
    });

    this.overlayRef.attach(this.portal());
    this.overlayRef.backdropClick().subscribe(() => this.close());
    this.overlayRef
      .keydownEvents()
      .subscribe((event) => this.handleKeyDown(event));
  }

  close() {
    this.searchForm.term().value.set('');
    this.isOpen.set(false);
    this.overlayRef?.detach();
    this.closed.emit();
  }

  select(option: AppUser) {
    this.selected.set(option);

    if (this.isOpen()) {
      this.selectChange.emit(option);
    }

    this.selected.set(null);
  }

  handleKeyDown(event: KeyboardEvent) {
    switch (event.key) {
      case 'ArrowUp':
        this.selectPreviousOption();
        break;
      case 'ArrowDown':
        this.selectNextOption();
        break;
      case 'Enter':
        this.confirmKeyboardSelection();
        break;
      case 'Escape':
        this.close();
        break;
    }
  }

  selectNextOption() {
    const options = this.filteredOptions();

    if (!this.selected()) {
      this.selected.set((options.length && options[0]) || null);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );
      if (options.length !== currentIndex + 1) {
        this.selected.set(options[currentIndex + 1]);
      }
    }
  }

  selectPreviousOption() {
    const options = this.filteredOptions();
    const optionsValue = this.options();
    if (!optionsValue) return;

    if (!this.selected()) {
      this.selected.set((options.length && options[0]) || null);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );
      if (currentIndex !== 0) {
        this.selected.set(optionsValue[currentIndex - 1]);
      }
    }
  }

  confirmKeyboardSelection() {
    const selected = this.selected();
    if (this.isOpen() && selected) {
      this.selectChange.emit(selected);
      this.selected.set(null);
    }
  }

  isActive(option: AppUser) {
    return option.id === this.selected()?.id;
  }

  isSelected(option: AppUser) {
    return this.valueIdSet().has(option.id);
  }

  search(value?: string | null) {
    const options = this.options();
    if (!options) return;

    if (!value) {
      this.filteredOptions.set(options);
    } else {
      this.filteredOptions.set(
        filterObjectArray(options, 'displayName', value)
      );
      this.selectNextOption();
    }
  }
}
