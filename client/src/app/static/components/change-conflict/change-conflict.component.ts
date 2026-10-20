import { Component, input, output } from '@angular/core';
import { LucideTriangleAlert } from '@lucide/angular';
import { InlineButtonComponent } from '../button/inline-button.component';
import { CalloutComponent } from '../callout/callout.component';

@Component({
  selector: 'app-change-conflict',
  template: `
    <app-callout
      color="warn"
      role="alert"
      [icon]="conflictIcon"
      [title]="title()">
      <span
        i18n="
          Body of the notice shown when something changed elsewhere while it was
          being edited
        ">
        Your edits aren't saved. Load the new version, or keep yours and save it
        over the change.
      </span>
      <span class="mt-2 flex gap-3">
        <button app-inline-button type="button" (click)="loadIncoming.emit()">
          <span
            i18n="
              Button that discards local edits and loads the version that
              changed elsewhere
            "
            >Load new version</span
          >
        </button>
        <button
          app-inline-button
          type="button"
          color="warn"
          (click)="keepMine.emit()">
          <span
            i18n="
              Button that saves local edits over the version that changed
              elsewhere
            "
            >Keep mine</span
          >
        </button>
      </span>
    </app-callout>
  `,
  imports: [CalloutComponent, InlineButtonComponent],
})
export class ChangeConflictComponent {
  readonly title = input(
    $localize`:Title of the notice shown when something changed elsewhere while it was being edited:This was changed elsewhere`
  );

  readonly loadIncoming = output();
  readonly keepMine = output();

  protected readonly conflictIcon = LucideTriangleAlert;
}
