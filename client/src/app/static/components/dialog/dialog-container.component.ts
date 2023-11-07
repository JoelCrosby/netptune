import { CdkDialogContainer } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dialog-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host {
        display: block;
        background: var(--background);
        border-radius: 8px;
        padding: 16px;
        box-shadow:
          0 11px 15px -7px #0003,
          0 24px 38px 3px #00000024,
          0 9px 46px 8px #0000001f;
        padding: 24px;
        border-radius: 4px;
        box-sizing: border-box;
        outline: 0;
        width: 100%;
        height: 100%;
        min-height: inherit;
        max-height: inherit;
      }

      .dialog-inner {
        max-height: 65vh;
        overflow: auto;
      }
    `,
  ],
  template: `<div class="dialog-inner custom-scroll">
    <ng-template cdkPortalOutlet></ng-template>
  </div>`,
})
export class DialogContainerComponent extends CdkDialogContainer {}
