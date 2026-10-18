import { Component, input, model } from '@angular/core';
import { SwitchComponent } from './switch.component';

@Component({
  selector: 'app-switch-field',
  imports: [SwitchComponent],
  host: { class: 'flex items-center justify-between gap-4' },
  template: `
    <div class="min-w-0">
      <p class="text-sm font-medium">{{ label() }}</p>
      @if (hint()) {
        <p class="text-muted text-xs">{{ hint() }}</p>
      }
    </div>

    <app-switch
      [ariaLabel]="label()"
      [disabled]="disabled()"
      [(checked)]="checked" />
  `,
})
export class SwitchFieldComponent {
  readonly label = input.required<string>();
  readonly hint = input('');
  readonly disabled = input(false);
  readonly checked = model(false);
}
