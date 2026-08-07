import { HttpClient } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { ExportFieldsStepComponent } from '@app/features/data-transfer/components/export-steps/export-fields-step.component';
import { ExportFilterStepComponent } from '@app/features/data-transfer/components/export-steps/export-filter-step.component';
import { ExportFormatStepComponent } from '@app/features/data-transfer/components/export-steps/export-format-step.component';
import { ExportReviewStepComponent } from '@app/features/data-transfer/components/export-steps/export-review-step.component';
import { ExportWhatStepComponent } from '@app/features/data-transfer/components/export-steps/export-what-step.component';
import {
  ExportLastStepIndex,
  ExportWizardService,
} from '@app/features/data-transfer/services/export-wizard.service';
import { ClientResponse } from '@core/models/client-response';
import {
  ExportDefinitionViewModel,
  ExportPreviewResult,
} from '@core/models/view-models/export-definition';
import { ExportJobViewModel } from '@core/models/view-models/export-job-view-model';
import { DialogService } from '@core/services/dialog.service';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import {
  SaveExportDefinitionDialogComponent,
  SaveExportDefinitionDialogResult,
} from '@entry/dialogs/save-export-definition-dialog/save-export-definition-dialog.component';
import { WizardActionsComponent } from '@app/features/data-transfer/components/wizard-actions.component';
import { LucideDownload, LucideFileDown, LucidePlay } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';
import { first } from 'rxjs';

@Component({
  selector: 'app-export-wizard-view',
  providers: [ExportWizardService],
  imports: [
    ChartCardComponent,
    ExportFieldsStepComponent,
    ExportFilterStepComponent,
    ExportFormatStepComponent,
    ExportReviewStepComponent,
    ExportWhatStepComponent,
    FlatButtonComponent,
    LucideDownload,
    LucidePlay,
    PageContainerComponent,
    PageHeaderComponent,
    StepComponent,
    StepperComponent,
    StrokedButtonComponent,
    WizardActionsComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for the guided export builder"
        title="Export" />

      <app-chart-card
        [clipContent]="false"
        [icon]="exportIcon"
        i18n-title="Heading of the export wizard card"
        title="Build an export"
        i18n-description="Explains what the export wizard produces"
        description="Choose the records and fields, then download the file or run it in the background.">
        <app-stepper mode="wizard" [(activeIndex)]="activeIndex">
          <app-step
            i18n-title="Wizard step that picks what to export"
            title="What">
            <app-export-what-step />
          </app-step>

          <app-step
            i18n-title="Wizard step that picks the exported fields"
            title="Fields">
            <app-export-fields-step [archiveFileBytes]="archiveFileBytes()" />
          </app-step>

          <app-step
            i18n-title="Wizard step that narrows what is exported"
            title="Filter">
            <app-export-filter-step />
          </app-step>

          <app-step
            i18n-title="Wizard step that picks the file format"
            title="Format">
            <app-export-format-step />
          </app-step>

          <app-step
            i18n-title="Wizard step that reviews and starts the export"
            title="Review">
            <app-export-review-step
              [preview]="preview()"
              [error]="error()"
              [active]="activeIndex() === lastStepIndex" />
          </app-step>
        </app-stepper>

        <app-wizard-actions
          [activeIndex]="wizard.activeIndex()"
          [lastStepIndex]="lastStepIndex"
          [blocker]="wizard.stepBlocker()"
          [canGoNext]="wizard.canGoNext()"
          (back)="wizard.back()"
          (next)="wizard.next()">
          @if (activeIndex() === lastStepIndex) {
            <div wizardActions class="flex flex-wrap items-center gap-3">
              <button
                app-stroked-button
                type="button"
                (click)="saveDefinition()">
                <span
                  i18n="Button that stores the current export setup for reuse">
                  Save as definition
                </span>
              </button>

              <button
                app-flat-button
                type="button"
                [disabled]="isBusy()"
                (click)="start()">
                @if (canRunInline()) {
                  <svg lucideDownload class="h-4 w-4"></svg>
                  <span i18n="Button that downloads an export straight away"
                    >Download</span
                  >
                } @else {
                  <svg lucidePlay class="h-4 w-4"></svg>
                  <span
                    i18n="
                      Button that queues an export to run in the background
                    ">
                    Start export
                  </span>
                }
              </button>
            </div>
          }
        </app-wizard-actions>
      </app-chart-card>
    </app-page-container>
  `,
})
export class ExportWizardViewComponent {
  protected readonly wizard = inject(ExportWizardService);
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(DialogService);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  protected readonly lastStepIndex = ExportLastStepIndex;
  protected readonly exportIcon = LucideFileDown;
  protected readonly activeIndex = this.wizard.activeIndex;
  protected readonly preview = signal<ExportPreviewResult | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly isBusy = signal(false);

  protected readonly archiveFileBytes = computed(() => {
    return this.preview()?.archiveFileBytes ?? 0;
  });

  protected readonly canRunInline = computed(() => {
    return this.preview()?.canRunInline ?? false;
  });

  constructor() {
    effect(() => {
      const isReviewStep = this.activeIndex() === this.lastStepIndex;
      const definition = this.wizard.definition();

      if (!isReviewStep) {
        return;
      }

      this.loadPreview(definition);
    });
  }

  protected start() {
    const canRunInline = this.canRunInline();

    if (canRunInline) {
      this.download();

      return;
    }

    this.queue();
  }

  protected saveDefinition() {
    const dialogRef = this.dialog.open<SaveExportDefinitionDialogResult>(
      SaveExportDefinitionDialogComponent,
      { width: '460px' }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        if (!result?.name) return;

        this.storeDefinition(result.name, result.isShared);
      },
    });
  }

  private storeDefinition(name: string, isShared: boolean) {
    this.http
      .post<ClientResponse<ExportDefinitionViewModel>>(
        'api/export/definitions',
        { name, isShared, definition: this.wizard.definition() }
      )
      .subscribe({
        next: () => this.wizard.savedDefinitions.reload(),
        error: () =>
          this.error.set(
            $localize`:Shown when a saved export setup could not be stored:The export could not be saved.`
          ),
      });
  }

  private loadPreview(
    definition: ReturnType<ExportWizardService['definition']>
  ) {
    this.error.set(null);

    this.http
      .post<ClientResponse<ExportPreviewResult>>('api/export/preview', {
        definition,
      })
      .subscribe({
        next: (response) => this.preview.set(response.payload ?? null),
        error: () => {
          this.preview.set(null);
          this.error.set(
            $localize`:Shown when an export preview could not be built:This export could not be previewed. Check the fields and filters.`
          );
        },
      });
  }

  private download() {
    this.isBusy.set(true);

    this.http
      .post(
        'api/export/run',
        { definition: this.wizard.definition() },
        {
          observe: 'response',
          responseType: 'blob',
        }
      )
      .subscribe({
        next: (response) => {
          this.isBusy.set(false);
          this.saveBlob(
            response.body,
            response.headers.get('content-disposition')
          );
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when an immediate export download failed:The export could not be downloaded.`
          );
        },
      });
  }

  private queue() {
    this.isBusy.set(true);

    this.http
      .post<ClientResponse<ExportJobViewModel>>('api/export/jobs', {
        definition: this.wizard.definition(),
      })
      .subscribe({
        next: () => {
          this.isBusy.set(false);
          this.router.navigate([
            '/',
            this.workspaceId(),
            'settings',
            'workspace',
            'data',
          ]);
        },
        error: () => {
          this.isBusy.set(false);
          this.error.set(
            $localize`:Shown when an export job could not be queued:The export could not be started.`
          );
        },
      });
  }

  private saveBlob(body: Blob | null, contentDisposition: string | null) {
    if (!body) return;

    const match = contentDisposition?.match(/filename="?([^";]+)"?/);
    const url = URL.createObjectURL(body);
    const link = document.createElement('a');

    link.href = url;
    link.download = match?.[1] ?? 'export';
    link.click();

    URL.revokeObjectURL(url);
  }
}
