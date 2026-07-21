import { booleanAttribute, Component, input } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { DialogCloseDirective } from '../../directives/dialog-close.directive';

@Component({
  selector: 'app-dialog-title',
  imports: [DialogCloseDirective, IconButtonComponent, LucideX],
  template: `
    <div class="relative mb-6">
      <h1 class="m-0 pr-10 text-xl font-medium"><ng-content /></h1>
      @if (showCloseButton()) {
        <button
          class="absolute -top-2 -right-2"
          app-icon-button
          app-dialog-close
          type="button"
          aria-label="Close dialog">
          <svg lucideX class="h-5 w-5" aria-hidden="true"></svg>
        </button>
      }
    </div>
  `,
})
export class DialogTitleComponent {
  readonly showCloseButton = input(false, { transform: booleanAttribute });
}
