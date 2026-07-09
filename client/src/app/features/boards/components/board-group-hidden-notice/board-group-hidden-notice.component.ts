import { Component, input, output } from '@angular/core';
import { LucideEye, LucideEyeOff } from '@lucide/angular';
import { FilterActionButtonComponent } from '@static/components/filter-action-button/filter-action-button.component';

@Component({
  selector: 'app-board-group-hidden-notice',
  imports: [FilterActionButtonComponent],
  template: `
    <app-filter-action-button
      label="Manage hidden groups"
      [icon]="count() > 0 ? lucideEyeOff : lucideEye"
      [color]="count() ? 'primary' : undefined"
      [count]="count()"
      (action)="manage.emit()" />
  `,
})
export class BoardGroupHiddenNoticeComponent {
  readonly count = input.required<number>();

  readonly manage = output();

  readonly lucideEye = LucideEye;
  readonly lucideEyeOff = LucideEyeOff;
}
