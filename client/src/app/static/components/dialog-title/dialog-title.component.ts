import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dialog-title',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<h1 class="m-0 mb-6 text-xl font-medium"><ng-content /></h1>`,
})
export class DialogTitleComponent {}
