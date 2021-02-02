import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import { FormControl } from '@angular/forms';
import { Workspace } from '@core/models/workspace';
import { filterObjectArray } from '@core/util/arrays';
import { UntilDestroy, untilDestroyed } from '@ngneat/until-destroy';
import { BehaviorSubject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';

@UntilDestroy()
@Component({
  selector: 'app-workspace-select',
  templateUrl: './workspace-select.component.html',
  styleUrls: ['./workspace-select.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkspaceSelectComponent implements OnInit {
  @Input() options: Workspace[] = [];
  @Input() value: number;

  @Output() selectChange = new EventEmitter();
  @Output() closed = new EventEmitter();

  searchControl = new FormControl();

  isOpen = false;
  selected: Workspace;

  get label() {
    return this.selected ? this.selected.name : 'Select...';
  }

  options$ = new BehaviorSubject<Workspace[]>([]);

  constructor() {}

  ngOnInit() {
    this.options$.next(this.options ?? []);

    this.searchControl.valueChanges
      .pipe(debounceTime(300), untilDestroyed(this))
      .subscribe((term) => this.search(term));
  }

  open(dropdown: HTMLElement, origin: HTMLElement) {
    this.isOpen = true;
    dropdown.style.width = `${origin.offsetWidth}px`;
  }

  close() {
    this.closed.emit();
    this.isOpen = false;
    this.searchControl.patchValue('');
  }

  select(option: Workspace) {
    this.selected = option;
    this.selectChange.emit(option.id);
    this.close();
  }

  isActive(option: Workspace) {
    if (!this.selected) {
      return false;
    }
    return option.id === this.selected.id;
  }

  search(value: string) {
    if (!value) {
      this.options$.next(this.options);
    } else {
      this.options$.next(filterObjectArray(this.options, 'name', value));
    }
  }
}
