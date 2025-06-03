/* eslint-disable @angular-eslint/no-host-metadata-property */

import { Highlightable } from '@angular/cdk/a11y';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  HostBinding,
  HostListener,
  Input,
} from '@angular/core';
import { FormSelectComponent } from './form-select.component';
import { FormSelectService } from './form-select.service';

@Component({
    selector: 'app-form-select-option',
    host: {
        class: 'nept-form-select-option',
    },
    template: `<ng-content />`,
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: false
})
export class FormSelectOptionComponent<TValue> implements Highlightable {
  @Input() value!: TValue;

  @HostBinding('class.selected')
  get selected(): boolean {
    return this.select?.selectedOption === this;
  }

  @HostBinding('class.active')
  active = false;

  get viewValue(): string {
    return (this.element?.nativeElement.textContent || '').trim();
  }

  private select?: FormSelectComponent<TValue>;

  constructor(
    private service: FormSelectService<TValue>,
    private element: ElementRef
  ) {
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

    this.select?.selectOption(this);
  }
}
