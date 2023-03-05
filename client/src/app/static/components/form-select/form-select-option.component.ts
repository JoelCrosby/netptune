import {
  ChangeDetectionStrategy,
  Component,
  HostBinding,
  HostListener,
  Input,
} from '@angular/core';
import { FormSelectComponent } from './form-select.component';
import { FormSelectService } from './form-select.service';

@Component({
  selector: 'app-form-select-option',
  template: `
    <div class="nept-form-select-option">
      <ng-content></ng-content>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormSelectOptionComponent {
  @Input() value: string | unknown;

  @HostBinding('class.selected')
  get selected(): boolean {
    return this.select?.selectedOption === this;
  }

  private select?: FormSelectComponent;

  constructor(private service: FormSelectService) {
    this.select = this.service.getSelect();
  }

  @HostListener('click', ['$event'])
  onClick(event: UIEvent) {
    event.preventDefault();
    event.stopPropagation();

    this.select?.selectOption(this);
  }
}
