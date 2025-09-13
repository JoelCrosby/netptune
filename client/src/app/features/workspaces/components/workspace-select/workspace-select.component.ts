import { ChangeDetectionStrategy, Component, ElementRef, EventEmitter, OnChanges, OnInit, Output, SimpleChanges, ViewChild, inject, input } from '@angular/core';
import { FormControl, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { logout } from '@core/auth/store/auth.actions';
import { Workspace } from '@core/models/workspace';
import { filterObjectArray } from '@core/util/arrays';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { Store } from '@ngrx/store';
import { BehaviorSubject, fromEvent } from 'rxjs';
import { debounceTime, filter, tap, throttleTime } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { AutofocusDirective } from '@static/directives/autofocus.directive';
import { RouterLink } from '@angular/router';

@UntilDestroy()
@Component({
  selector: 'app-workspace-select',
  templateUrl: './workspace-select.component.html',
  styleUrls: ['./workspace-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    AutofocusDirective,
    ReactiveFormsModule,
    RouterLink,
    AsyncPipe
],
})
export class WorkspaceSelectComponent implements OnInit, OnChanges {
  private store = inject(Store);

  @ViewChild('dropdown') dropdownElementRef!: ElementRef;

  readonly options = input<Workspace[] | null>([]);
  readonly value = input<string | null>();
  readonly compact = input(false);

  @Output() selectChange = new EventEmitter<Workspace>();
  @Output() closed = new EventEmitter();

  searchControl = new FormControl();

  isOpen = false;
  currentWorkspace: Workspace | null = null;
  selected: Workspace | null = null;

  options$ = new BehaviorSubject<Workspace[]>([]);

  ngOnInit() {
    this.searchControl.valueChanges
      .pipe(debounceTime(300), untilDestroyed(this))
      .subscribe((term: string) => this.search(term));

    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        untilDestroyed(this),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();

    fromEvent<KeyboardEvent>(document, 'keydown', {
      passive: true,
    })
      .pipe(
        untilDestroyed(this),
        filter(() => this.isOpen),
        tap(this.handleKeyDown.bind(this))
      )
      .subscribe();

    this.options$.next(this.options() ?? []);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes.value || changes.options) {
      const options = this.options();
      if (this.value() && !this.currentWorkspace && options) {
        const option = options.find((opt) => opt.slug === this.value());
        this.select(option);
      }
    }
  }

  handleDocumentClick(event: Event) {
    if (!this.dropdownElementRef.nativeElement.contains(event.target)) {
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
    const options = this.options$.value;

    if (!this.selected) {
      this.selected = (options.length && options[0]) || null;
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected?.id
      );

      if (options.length === currentIndex + 1) {
        return;
      }

      this.selected = options[currentIndex + 1];
    }

    this.options$.next(options);
  }

  selectPreviousOption() {
    const options = this.options$.value;

    const optionsValue = this.options();
    if (!optionsValue) return;

    if (!this.selected) {
      this.selected = (options.length && options[0]) || null;
    } else {
      const currentIndex = options.findIndex(
        (opt) => opt.id === this.selected?.id
      );

      if (currentIndex === 0) return;

      this.selected = optionsValue[currentIndex - 1];
    }

    this.options$.next(options);
  }

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen = true;
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
    this.isOpen = false;
    this.searchControl.patchValue('');
  }

  select(option: Workspace | null = null) {
    this.selected = option ?? this.selected;
    this.currentWorkspace = this.selected;

    if (this.isOpen && this.selected) {
      this.selectChange.emit(this.selected);
      this.close();
    }

    this.selected = null;
  }

  isActive(option: Workspace) {
    if (!this.selected) {
      return false;
    }
    return option.id === this.selected.id;
  }

  search(value: string) {
    const options = this.options();
    if (!options) return;

    if (!value) {
      this.options$.next(options);
    } else {
      this.options$.next(filterObjectArray(options, 'name', value));
      this.selectNextOptiom();
    }
  }

  onlogOutClicked() {
    this.close();
    this.store.dispatch(logout());
  }
}
