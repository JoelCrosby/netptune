import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dialog-content',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <div class="dialog-content">
    <ng-content />
  </div>`,
})
export class DialogContentComponent {}
