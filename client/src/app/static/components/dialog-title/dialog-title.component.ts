import { booleanAttribute, Component, input } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { IconButtonComponent } from '../button/icon-button.component';
import { DialogCloseDirective } from '../../directives/dialog-close.directive';

@Component({
  selector: 'app-dialog-title',
  imports: [DialogCloseDirective, IconButtonComponent, LucideX],
  host: { class: 'block' },
  template: `
    <div class="relative" [class.mb-6]="!noMargin()">
      <h1 class="m-0 pr-10 text-xl font-medium"><ng-content /></h1>
      @if (showCloseButton()) {
        <button
          class="absolute -top-2 -right-2"
          app-icon-button
          app-dialog-close
          type="button"
          i18n-aria-label="Accessible label for the button that closes a dialog"
          aria-label="Close dialog">
          <svg lucideX class="h-5 w-5" aria-hidden="true"></svg>
        </button>
      }
    </div>
  `,
})
export class DialogTitleComponent {
  readonly showCloseButton = input(false, { transform: booleanAttribute });
  readonly noMargin = input(false, { transform: booleanAttribute });
}
