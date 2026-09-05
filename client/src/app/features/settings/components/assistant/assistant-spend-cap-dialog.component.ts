import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AiSpendService } from '@core/services/ai-spend.service';
import { getErrorMessage } from '@core/util/error-message';
import { formatCurrency } from '@core/util/ai-usage';
import { LucideInfo } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { first } from 'rxjs';

export interface AssistantSpendCapDialogData {
  cap: number | null;
  monthToDate: number;
}

@Component({
  selector: 'app-assistant-spend-cap-dialog',
  imports: [
    CalloutComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    FlatButtonComponent,
    FormsModule,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title
      showCloseButton
      i18n="Title of the dialog that sets the assistant spend cap">
      Monthly spend cap
    </app-dialog-title>

    <div class="flex flex-col gap-5">
      <label class="flex flex-col gap-1.5">
        <span class="text-muted text-xs" i18n="Label for the spend cap input">
          Cap in US dollars
        </span>
        <div
          class="border-border bg-background flex h-9 items-center gap-1 rounded border px-3 focus-within:border-transparent">
          <span class="text-muted text-sm">$</span>
          <input
            type="number"
            name="assistant-spend-cap"
            class="placeholder:text-muted min-w-0 flex-1 bg-transparent tabular-nums outline-none"
            min="1"
            step="1"
            i18n-placeholder="Placeholder shown when no spend cap is set"
            placeholder="No cap"
            [ngModel]="cap()"
            (ngModelChange)="cap.set($event)" />
        </div>
        <span class="text-muted text-xs" i18n="Explains how to clear the cap">
          Leave this empty for no cap.
        </span>
      </label>

      <app-callout [icon]="infoIcon" color="primary">
        <p i18n="Explains what happens when the spend cap is reached">
          {{ spentLabel() }} has been spent this month. Once the cap is reached
          the assistant stops accepting new messages until the next month.
        </p>
      </app-callout>

      @if (error(); as message) {
        <app-callout color="warn" role="alert">
          <p>{{ message }}</p>
        </app-callout>
      }
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Cancel</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="busy()"
        (click)="save()">
        <span i18n="Button that stores the assistant spend cap">Save cap</span>
      </button>
    </div>
  `,
})
export class AssistantSpendCapDialogComponent {
  private readonly data = inject<AssistantSpendCapDialogData>(DIALOG_DATA);
  private readonly dialogRef =
    inject<DialogRef<boolean, AssistantSpendCapDialogComponent>>(DialogRef);

  private readonly service = inject(AiSpendService);
  private readonly snackbar = inject(SnackbarService);

  protected readonly infoIcon = LucideInfo;
  protected readonly cap = signal<number | null>(this.data.cap);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly spentLabel = computed(() => {
    return formatCurrency(this.data.monthToDate);
  });

  protected save() {
    const cap = this.normalisedCap();
    const isInvalid = cap !== null && cap <= 0;

    if (isInvalid) {
      this.error.set(
        $localize`:Shown when the spend cap is not a positive amount:A cap has to be more than $0. Leave it empty for no cap.`
      );

      return;
    }

    this.busy.set(true);
    this.error.set(null);

    this.service
      .setCap({ cap })
      .pipe(first())
      .subscribe({
        next: (response) => {
          this.busy.set(false);

          if (!response.isSuccess) {
            this.error.set(
              response.message ??
                $localize`:Shown when saving the spend cap fails:The cap could not be saved.`
            );

            return;
          }

          this.snackbar.success(
            $localize`:Shown after the assistant spend cap is stored:Spend cap saved.`
          );
          this.dialogRef.close(true);
        },
        error: (error) => {
          this.busy.set(false);
          this.error.set(
            getErrorMessage(
              error,
              $localize`:Shown when saving the spend cap fails:The cap could not be saved.`
            )
          );
        },
      });
  }

  private normalisedCap(): number | null {
    const value = this.cap();
    const isEmpty = value === null || value === undefined || isNaN(value);

    return isEmpty ? null : Number(value);
  }
}
