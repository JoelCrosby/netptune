import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
    selector: 'app-form-error',
    changeDetection: ChangeDetectionStrategy.OnPush,
    template: `
    <div class="form-error">
      <ng-content />
    </div>
  `
})
export class FormErrorComponent {}
