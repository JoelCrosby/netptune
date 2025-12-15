import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
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
import { debounce, form, Field } from '@angular/forms/signals';
import { AppUser } from '@core/models/appuser';
import { AssigneeViewModel } from '@core/models/view-models/board-view';
import { filterObjectArray } from '@core/util/arrays';
import { DocumentService } from '@static/services/document.service';
import { KeyboardService } from '@static/services/keyboard.service';
import { AutofocusDirective } from '../../directives/autofocus.directive';
import { AvatarComponent } from '../avatar/avatar.component';

type User = AssigneeViewModel | AppUser;

@Component({
  selector: 'app-user-select',
  templateUrl: './user-select.component.html',
  styleUrls: ['./user-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AvatarComponent, AutofocusDirective, Field],
})
export class UserSelectComponent {
  private document = inject(DocumentService);
  private keyboard = inject(KeyboardService);
  readonly dropdownElementRef = viewChild.required<ElementRef>('dropdown');

  readonly options = input<AppUser[] | null>([]);
  readonly value = model<User[]>([]);
  readonly compact = input(false);
  readonly label = input('Select Users');
  readonly noResults = input('No results found...');

  readonly selectChange = output<AppUser>();
  readonly closed = output();

  isOpen = signal(false);
  selected = signal<AppUser | null>(null);

  filterdOptions = signal<AppUser[]>(this.options() ?? []);
  valueIdSet = computed(() => {
    const ids = this.value().map((v) => v.id);
    return new Set<string>(ids);
  });

  searchFormModel = signal({
    term: '',
  });

  searchForm = form(this.searchFormModel, (schema) => {
    debounce(schema.term, 300);
  });

  constructor() {
    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });

    effect(() => {
      const event = this.keyboard.keyDown();

      untracked(() => {
        if (event && this.isOpen()) {
          this.handleKeyDown(event);
        }
      });
    });

    effect(() => {
      const term = this.searchForm.term().value();
      untracked(() => this.search(term));
    });
  }

  handleDocumentClick(event: EventTarget) {
    if (!this.isOpen()) {
      return;
    }

    if (!this.dropdownElementRef().nativeElement.contains(event)) {
      this.close();
    }
  }

  handleKeyDown(event: KeyboardEvent) {
    switch (event.key) {
      case 'ArrowUp':
        this.selectPreviousOption();
        break;
      case 'ArrowDown':
        this.selectNextOptiom();
        break;
      case 'Enter':
        this.select();
        break;
    }
  }

  selectNextOptiom() {
    const options = this.filterdOptions();

    if (!this.selected()) {
      this.selected.set((options.length && options[0]) || null);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );

      if (options.length === currentIndex + 1) {
        return;
      }

      this.selected.set(options[currentIndex + 1]);
    }

    this.filterdOptions.set(options);
  }

  selectPreviousOption() {
    const options = this.filterdOptions();

    const optionsValue = this.options();
    if (!optionsValue) return;

    if (!this.selected()) {
      this.selected.set((options.length && options[0]) || null);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );

      if (currentIndex === 0) return;

      this.selected.set(optionsValue[currentIndex - 1]);
    }

    this.filterdOptions.set(options);
  }

  open(event: Event, dropdown: HTMLElement, origin: HTMLElement) {
    event.stopPropagation();
    event.preventDefault();

    this.isOpen.set(true);

    if (this.compact()) {
      dropdown.style.width = '200px';
      dropdown.style.left = '80px';
      dropdown.style.top = '0px';
    } else {
      dropdown.style.width = `${origin.offsetWidth}px`;
      dropdown.style.left = '0';
      dropdown.style.top = '42px';
    }
  }

  close() {
    this.searchForm.term().value.set('');
    this.isOpen.set(false);

    this.closed.emit();
  }

  select(option: AppUser | null = null) {
    this.selected.set(option ?? this.selected());
    const selected = this.selected();

    if (this.isOpen() && selected) {
      this.selectChange.emit(selected);
    }

    this.selected.set(null);
  }

  isActive(option: AppUser) {
    const selected = this.selected();

    if (!selected) {
      return false;
    }

    return option.id === selected.id;
  }

  isSelected(option: AppUser) {
    return this.valueIdSet().has(option.id);
  }

  search(value?: string | null) {
    const options = this.options();
    if (!options) return;

    if (!value) {
      this.filterdOptions.set(options);
    } else {
      this.filterdOptions.set(filterObjectArray(options, 'displayName', value));
      this.selectNextOptiom();
    }
  }
}
