import { Component, inject } from '@angular/core';
import { ImportWizardService } from '@app/features/data-transfer/services/import-wizard.service';
import {
  ImportSessionViewModel,
  ImportStage,
} from '@core/models/view-models/import-session';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';

@Component({
  selector: 'app-import-run-step',
  imports: [
    ProgressBarComponent,
    SectionHeaderComponent,
    StatStripComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Heading of the import run step"
      heading="Running the import"
      i18n-description="Explains the import run step"
      description="Progress and the result. A finished import can be undone." />

    @if (wizard.session(); as current) {
      <app-progress-bar [value]="current.progressPercent" />

      <p class="text-muted mt-2 text-sm">
        {{ current.progressMessage }} · {{ current.progressPercent }}%
      </p>

      @if (wizard.stalled()) {
        <p class="text-warn mt-2 text-sm" role="status">
          <span
            i18n="Shown when a committed import has not progressed for a while">
            This import has not moved for a while. It runs on the job server —
            check that the job server is running.
          </span>
        </p>
      }

      @if (current.stage === committedStage) {
        <div class="-mx-6 mt-4">
          <app-stat-strip [items]="resultStats(current)" />
        </div>

        <button
          app-stroked-button
          class="mt-4"
          type="button"
          (click)="wizard.undo()">
          <span i18n="Button that reverses a finished import">
            Undo this import
          </span>
        </button>
      }

      @if (!wizard.isRunning()) {
        <button
          app-stroked-button
          class="mt-4 ml-2"
          type="button"
          (click)="wizard.refresh()">
          <span i18n="Button that re-reads the import progress">Refresh</span>
        </button>
      }
    } @else {
      <p
        class="text-muted text-sm"
        i18n="Explains why the import run step is empty">
        Nothing is running yet. Finish the earlier steps and press Import.
      </p>
    }
  `,
})
export class ImportRunStepComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected readonly committedStage = ImportStage.committed;

  protected resultStats(current: ImportSessionViewModel): StatStripItem[] {
    return [
      {
        label: $localize`:Counts records an import created:Created`,
        value: current.created,
      },
      {
        label: $localize`:Counts records an import updated:Updated`,
        value: current.updated,
      },
      {
        label: $localize`:Counts records an import skipped:Skipped`,
        value: current.skipped,
      },
    ];
  }
}
