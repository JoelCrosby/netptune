import { Component, computed, inject } from '@angular/core';
import { ImportMappingStepComponent } from '@app/features/data-transfer/components/import-steps/import-mapping-step.component';
import { ImportPreviewStepComponent } from '@app/features/data-transfer/components/import-steps/import-preview-step.component';
import { ImportRunStepComponent } from '@app/features/data-transfer/components/import-steps/import-run-step.component';
import { ImportSourceStepComponent } from '@app/features/data-transfer/components/import-steps/import-source-step.component';
import { ImportUploadStepComponent } from '@app/features/data-transfer/components/import-steps/import-upload-step.component';
import {
  ImportLastStepIndex,
  ImportWizardService,
} from '@app/features/data-transfer/services/import-wizard.service';
import { WizardActionsComponent } from '@app/features/data-transfer/components/wizard-actions.component';
import { LucideFileUp, LucidePlay } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';

const PreviewStepIndex = 3;

@Component({
  selector: 'app-import-wizard-view',
  providers: [ImportWizardService],
  imports: [
    ChartCardComponent,
    ImportMappingStepComponent,
    ImportPreviewStepComponent,
    ImportRunStepComponent,
    ImportSourceStepComponent,
    ImportUploadStepComponent,
    FlatButtonComponent,
    LucidePlay,
    PageContainerComponent,
    PageHeaderComponent,
    StepComponent,
    StepperComponent,
    WizardActionsComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the guided import builder"
        title="Import" />

      <app-chart-card
        [clipContent]="false"
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

        <app-wizard-actions
          [activeIndex]="wizard.activeIndex()"
          [lastStepIndex]="lastStepIndex"
          [blocker]="wizard.stepBlocker()"
          [canGoNext]="wizard.canGoNext()"
          [showNext]="showNext()"
          (back)="wizard.back()"
          (next)="wizard.next()">
          @if (wizard.activeIndex() === previewStepIndex) {
            <button
              wizardActions
              app-flat-button
              type="button"
              [disabled]="!wizard.canCommit() || wizard.isBusy()"
              (click)="wizard.commit()">
              <svg lucidePlay class="h-4 w-4"></svg>
              <span i18n="Button that starts an import">Import</span>
            </button>
          }
        </app-wizard-actions>
      </app-chart-card>
    </app-page-container>
  `,
})
export class ImportWizardViewComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected readonly lastStepIndex = ImportLastStepIndex;
  protected readonly importIcon = LucideFileUp;

  protected readonly previewStepIndex = PreviewStepIndex;

  protected readonly showNext = computed(() => {
    return this.wizard.activeIndex() !== PreviewStepIndex;
  });
}
