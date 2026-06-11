import { Component } from '@angular/core';

@Component({
  selector: 'app-dialog-content',
  template: ` <div class="dialog-content">
    <ng-content />
  </div>`,
})
export class DialogContentComponent {}
