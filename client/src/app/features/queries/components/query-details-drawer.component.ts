import { Component, input, model, output } from '@angular/core';
import { LucideInfo } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormControlFieldComponent } from '@static/components/form-control/form-control-field.component';
import { FormControlLabelDirective } from '@static/components/form-control/form-control.directives';
import { FormInputComponent } from '@static/components/form-input/form-input.component';

/**
 * The parts of a saved query that are not its conditions: description, visibility, and the notes explaining
 * what saving will actually do. Kept out of the editor so that page is layout and query only.
 */
@Component({
  selector: 'app-query-details-drawer',
  imports: [
    CalloutComponent,
    CheckboxComponent,
    FormControlFieldComponent,
    FormControlLabelDirective,
    FormInputComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <div
      class="border-border bg-card mb-3 grid items-end gap-4 rounded-xl border px-[18px] py-4 md:grid-cols-[1fr_1fr_auto]">
      <app-form-input
        density="compact"
        i18n-label="Label of the query description field"
        label="Description"
        name="query-description"
        [noMargin]="true"
        [(value)]="description" />

      <div>
        <span
          appFormLabel
          variant="compact"
          i18n="Label of the query visibility field">
          Visibility
        </span>

        <app-form-control-field density="compact">
          <app-checkbox
            class="w-full px-3 text-sm"
            [checked]="isShared()"
            [disabled]="!canManageShared()"
            (checkedChange)="isShared.set($event)">
            <span i18n="Checkbox that shares a query with the whole workspace">
              Share with the workspace
            </span>
          </app-checkbox>
        </app-form-control-field>
      </div>

      <button
        app-stroked-button
        color="neutral"
        class="h-9.5 rounded-lg"
        type="button"
        (click)="closed.emit()">
        <span i18n="Button that closes the query details drawer">Done</span>
      </button>
    </div>

    @if (!canManageShared()) {
      <p class="text-foreground/50 mb-3 text-xs">
        <span i18n="Explains why the share control is unavailable to this user">
          Sharing a query with the workspace needs the shared-queries
          permission.
        </span>
      </p>
    }

    @if (savesAsCopy()) {
      <app-callout color="primary" class="mb-3" [icon]="infoIcon">
        <span i18n="Shown when editing a shared query the user cannot change">
          You cannot change this shared query, so saving creates your own copy
          of it.
        </span>
      </app-callout>
    }
  `,
})
export class QueryDetailsDrawerComponent {
  readonly description = model.required<string>();
  readonly isShared = model.required<boolean>();
  readonly canManageShared = input(false);
  readonly savesAsCopy = input(false);

  readonly closed = output();

  protected readonly infoIcon = LucideInfo;
}
