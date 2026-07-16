import { CdkDialogContainer, ɵɵCdkPortalOutlet } from '@angular/cdk/dialog';
import { Component, InjectionToken, inject } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { DialogCloseDirective } from '../../directives/dialog-close.directive';
import { IconButtonComponent } from '../button/icon-button.component';

export const DIALOG_WIZARD_TITLE = new InjectionToken<string>(
  'DialogWizardTitle'
);

@Component({
  selector: 'app-dialog-wizard',
  imports: [
    ɵɵCdkPortalOutlet,
    IconButtonComponent,
    DialogCloseDirective,
    LucideX,
  ],
  host: {
    class:
      'border-border bg-dialog-background flex h-full w-full flex-col overflow-hidden rounded border shadow-2xl outline-none',
  },
  template: `
    <header
      class="border-border flex min-h-10 shrink-0 items-center justify-between gap-4 border-b px-8 py-2">
      <h1 class="m-0 min-w-0 truncate">
        {{ title }}
      </h1>
      <button
        app-icon-button
        app-dialog-close
        class="shrink-0"
        aria-label="Close dialog">
        <svg lucideX class="h-5 w-5" aria-hidden="true"></svg>
      </button>
    </header>

    <div class="custom-scroll min-h-0 min-w-0 flex-1 overflow-auto p-8">
      <ng-template cdkPortalOutlet></ng-template>
    </div>
  `,
})
export class DialogWizardComponent extends CdkDialogContainer {
  readonly title = inject(DIALOG_WIZARD_TITLE);
}
