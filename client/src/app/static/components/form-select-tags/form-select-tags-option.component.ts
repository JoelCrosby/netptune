import { Highlightable } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostBinding,
  HostListener,
  inject,
  input,
} from '@angular/core';
import { FormSelectTagsComponent } from './form-select-tags.component';
import { FormSelectTagsService } from './form-select-tags.service';

@Component({
  selector: 'app-form-select-tags-option',
  host: {
    class: 'nept-form-select-option',
  },
  template: `<ng-content />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectTagsOptionComponent<TValue> implements Highlightable {
  private service = inject<FormSelectTagsService<TValue>>(
    FormSelectTagsService
  );
  private element = inject(ElementRef);

  readonly value = input.required<TValue>();

  @HostBinding('class.selected')
  get selected(): boolean {
    return this.select?.isSelected(this.value()) ?? false;
  }

  @HostBinding('class.hidden')
  get hiddenBySearch(): boolean {
    const query = this.select?.searchQuery() ?? '';
    if (!query) return false;
    return !this.viewValue.toLowerCase().includes(query.toLowerCase());
  }

  @HostBinding('class.active')
  active = false;

  get viewValue(): string {
    return (this.element?.nativeElement.textContent || '').trim();
  }

  private select?: FormSelectTagsComponent<TValue>;

  constructor() {
    this.select = this.service.getSelect();
  }

  setActiveStyles(): void {
    this.active = true;
  }

  setInactiveStyles(): void {
    this.active = false;
  }

  disabled?: boolean | undefined;

  getLabel?(): string {
    return this.viewValue;
  }

  @HostListener('click', ['$event'])
  onClick(event: UIEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.select?.toggleOption(this);
  }
}
