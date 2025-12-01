import { CdkDialogContainer, ɵɵCdkPortalOutlet } from '@angular/cdk/dialog';
import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-dialog-container',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      :host {
        display: block;
        background: var(--dialog-background);
        border-radius: 8px;
        border: 1px solid var(--border);
        box-shadow:
          0 11px 15px -7px #0003,
          0 24px 38px 3px #00000024,
          0 9px 46px 8px #0000001f;
        padding: 24px 4px 24px 24px;
        border-radius: 4px;
        box-sizing: border-box;
        outline: 0;
        width: 100%;
        height: 100%;
        min-height: inherit;
        max-height: inherit;
      }

      .dialog-inner {
        padding-right: 16px;
      }
    `,
  ],
  template: `<div class="dialog-inner custom-scroll">
    <ng-template cdkPortalOutlet></ng-template>
  </div>`,
  imports: [ɵɵCdkPortalOutlet],
})
export class DialogContainerComponent extends CdkDialogContainer {}
