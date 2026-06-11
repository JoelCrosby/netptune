import { Component } from '@angular/core';

@Component({
  selector: 'app-form-error',
  template: `
    <div class="form-error">
      <ng-content />
    </div>
  `,
})
export class FormErrorComponent {}
