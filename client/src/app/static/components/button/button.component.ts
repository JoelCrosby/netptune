import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  HostBinding,
  Input,
  Output,
} from '@angular/core';

type ButtonColor = 'primary' | 'accent' | 'warn' | undefined;
type DefaultProp = string | boolean | null | undefined;

@Component({
  selector: 'app-button',
  template: `<button
    class="px-4 py-2 border-slate-100 rounded"
    (onclick)="onClicked($event)"
  >
    <ng-content></ng-content>
  </button>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonComponent {
  @Input() stroked: DefaultProp = false;
  @Input() flat: DefaultProp = false;
  @Input() disabled?: DefaultProp;
  @Input() color?: ButtonColor;

  // eslint-disable-next-line @angular-eslint/no-output-native
  @Output() click = new EventEmitter<MouseEvent>();

  constructor(public element: ElementRef) {}

  @HostBinding('click')
  onClicked(event: Event) {
    this.click.emit(event as MouseEvent);
  }
}
