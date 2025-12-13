import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { debounce, Field, form } from '@angular/forms/signals';
import { RouterLink } from '@angular/router';
import { logout } from '@core/auth/store/auth.actions';
import { Workspace } from '@core/models/workspace';
import { filterObjectArray } from '@core/util/arrays';
import { UntilDestroy } from '@ngneat/until-destroy';
import { Store } from '@ngrx/store';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { DocumentService } from '@static/services/document.service';
import { KeyboardService } from '@static/services/keyboard.service';

@UntilDestroy()
@Component({
  selector: 'app-workspace-select',
  templateUrl: './workspace-select.component.html',
  styleUrls: ['./workspace-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Field, AutofocusDirective, RouterLink],
})
export class WorkspaceSelectComponent {
  private store = inject(Store);
  private document = inject(DocumentService);
  private keyboard = inject(KeyboardService);

  readonly dropdownElementRef = viewChild.required<ElementRef>('dropdown');

  readonly options = model<Workspace[] | null>([]);
  readonly filteredOptions = model<Workspace[] | null>([]);
  readonly value = input<string | null>();
  readonly compact = input(false);

  readonly selectChange = output<Workspace>();
  readonly closed = output();

  searchFormModel = signal({
    term: '',
  });

  searchForm = form(this.searchFormModel, (schema) => {
    debounce(schema.term, 300);
  });

  isOpen = signal(false);
  currentWorkspace = signal<Workspace | null>(null);
  selected = signal<Workspace | null>(null);

  constructor() {
    effect(() => {
      this.search(this.searchForm.term().value());
    });

    effect(() => this.filteredOptions.set(this.options()));

    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });

    effect(() => {
      const event = this.keyboard.keyDown();
      untracked(() => {
        if (this.isOpen()) {
          this.handleKeyDown(event);
        }
      });
    });
  }

  handleDocumentClick(target: EventTarget) {
    if (!this.dropdownElementRef().nativeElement.contains(target)) {
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
    const options = this.filteredOptions() ?? [];

    if (!this.selected()) {
      const firstOption = options[0];
      this.selected.set(firstOption);
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected()?.id
      );

      if (options.length === currentIndex + 1) {
        return;
      }

      this.selected.set(options[currentIndex + 1]);
    }
  }

  selectPreviousOption() {
    const options = this.filteredOptions();
    const selected = this.selected();

    if (!options) return;

    if (!selected) {
      this.selected.set((options?.length && options[0]) || null);
    } else {
      const index = options?.findIndex((opt) => opt.id === selected.id) ?? -1;

      if (index === 0 || index === -1) {
        return;
      }

      this.selected.set(options[index - 1]);
    }
  }

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen.set(true);

    if (this.compact()) {
      dropdown.style.width = '200px';
      dropdown.style.left = '80px';
      dropdown.style.top = '0px';
    } else {
      dropdown.style.width = `${origin.offsetWidth}px`;
      dropdown.style.left = '0';
      dropdown.style.top = '55px';
    }
  }

  close() {
    this.closed.emit();
    this.isOpen.set(false);
    this.searchForm.term().value.set('');
  }

  select(option: Workspace | null = null) {
    this.selected.set(option ?? this.selected());
    this.currentWorkspace.set(this.selected());

    const selected = this.selected();

    if (this.isOpen() && selected) {
      this.selectChange.emit(selected);
      this.close();
    }

    this.selected.set(null);
  }

  isActive(option: Workspace) {
    if (!this.selected()) {
      return false;
    }
    return option.id === this.selected()?.id;
  }

  search(value: string) {
    const options = this.options();
    if (!options) return;

    if (!value) {
      this.filteredOptions.set(options);
    } else {
      this.filteredOptions.set(filterObjectArray(options, 'name', value));
      this.selectNextOptiom();
    }
  }

  onlogOutClicked() {
    this.close();
    this.store.dispatch(logout());
  }
}
