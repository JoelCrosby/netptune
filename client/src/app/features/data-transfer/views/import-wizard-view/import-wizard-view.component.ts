import { Component, inject } from '@angular/core';
import { ImportMappingStepComponent } from '@app/features/data-transfer/components/import-steps/import-mapping-step.component';
import { ImportPreviewStepComponent } from '@app/features/data-transfer/components/import-steps/import-preview-step.component';
import { ImportRunStepComponent } from '@app/features/data-transfer/components/import-steps/import-run-step.component';
import { ImportSourceStepComponent } from '@app/features/data-transfer/components/import-steps/import-source-step.component';
import { ImportUploadStepComponent } from '@app/features/data-transfer/components/import-steps/import-upload-step.component';
import {
  ImportLastStepIndex,
  ImportWizardService,
} from '@app/features/data-transfer/services/import-wizard.service';
import { LucideFileUp } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';

@Component({
  selector: 'app-import-wizard-view',
  providers: [ImportWizardService],
  imports: [
    ChartCardComponent,
    FlatButtonComponent,
    ImportMappingStepComponent,
    ImportPreviewStepComponent,
    ImportRunStepComponent,
    ImportSourceStepComponent,
    ImportUploadStepComponent,
    PageContainerComponent,
    PageHeaderComponent,
    StepComponent,
    StepperComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the guided import builder"
        title="Import" />

      <app-chart-card
        [icon]="importIcon"
        i18n-title="Heading of the import wizard card"
        title="Import a file"
        i18n-description="Explains what the import wizard does"
        description="Upload a file, map its columns onto fields, then check what will change before committing.">
        <app-stepper mode="wizard" [(activeIndex)]="wizard.activeIndex">
          <app-step
            i18n-title="Wizard step that uploads the file to import"
            title="Upload">
            <app-import-upload-step />
          </app-step>

          <app-step
            i18n-title="Wizard step showing what was detected in the file"
            title="Source">
            <app-import-source-step />
          </app-step>

          <app-step
            i18n-title="Wizard step that maps columns onto fields"
            title="Mapping">
            <app-import-mapping-step />
          </app-step>

          <app-step
            i18n-title="Wizard step that shows what an import would do"
            title="Preview">
            <app-import-preview-step />
          </app-step>

          <app-step i18n-title="Wizard step that runs the import" title="Run">
            <app-import-run-step />
          </app-step>
        </app-stepper>

        @if (wizard.error(); as message) {
          <p class="text-warn mt-4 text-sm" role="alert">{{ message }}</p>
        }

        <div
          class="border-border mt-6 flex flex-wrap items-center justify-between gap-3 border-t pt-4">
          <button
            app-stroked-button
            type="button"
            [disabled]="wizard.activeIndex() === 0"
            (click)="wizard.back()">
            <span i18n="Button that moves back one wizard step">Back</span>
          </button>

          @if (wizard.activeIndex() !== lastStepIndex) {
            <div class="flex min-w-0 items-center gap-3">
              @if (wizard.stepBlocker(); as reason) {
                <p class="text-muted text-xs" role="status">{{ reason }}</p>
              }

              <button
                app-flat-button
                type="button"
                [disabled]="!wizard.canGoNext()"
                (click)="wizard.next()">
                <span i18n="Button that moves forward one wizard step"
                  >Next</span
                >
              </button>
            </div>
          }
        </div>
      </app-chart-card>
    </app-page-container>
  `,
})
export class ImportWizardViewComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected readonly lastStepIndex = ImportLastStepIndex;
  protected readonly importIcon = LucideFileUp;
}
