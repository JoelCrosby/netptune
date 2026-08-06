import { Component, computed, inject, input, output } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { ExportPreviewResult } from '@core/models/view-models/export-definition';
import { ExportFormat } from '@core/models/view-models/export-job-view-model';
import { LucideDownload, LucidePlay } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import { DatatableDataSource } from '@static/components/datatable/datatable.types';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';

interface ExportPreviewRow {
  ref: string;
  values: Record<string, string>;
}

function formatName(format: ExportFormat): string {
  return ExportFormat[format].toUpperCase();
}

@Component({
  selector: 'app-export-review-step',
  imports: [
    DatatableComponent,
    FlatButtonComponent,
    LucideDownload,
    LucidePlay,
    SectionHeaderComponent,
    StatStripComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Heading of the export review step"
      heading="Review"
      i18n-description="Explains the export review step"
      description="A sample of the first rows this export produces." />

    @if (preview(); as result) {
      <div class="-mx-6 mb-4">
        <app-stat-strip [items]="reviewStats(result)" />
      </div>

      @if (!result.canRunInline) {
        <p
          class="text-muted mb-4 text-xs"
          i18n="Explains why an export has to run as a job">
          This export is too large to download directly and will run in the
          background.
        </p>
      }

      @if (!wizard.isArchive() && active()) {
        <div class="-mx-6">
          <app-datatable
            i18n-itemLabel="Names the rows of the export preview table"
            itemLabel="rows"
            containerClass="overflow-x-auto rounded-none border-x-0"
            tableClass="min-w-[900px]"
            [rounded]="false"
            [data]="previewData()" />
        </div>
      }
    } @else {
      <p class="text-muted text-sm" i18n="Shown while an export preview loads">
        Building a preview…
      </p>
    }

    @if (error(); as message) {
      <p class="text-warn mt-4 text-sm" role="alert">{{ message }}</p>
    }

    <div class="mt-6 flex flex-wrap gap-3">
      <button
        app-flat-button
        type="button"
        [disabled]="isBusy()"
        (click)="startExport.emit()">
        @if (canRunInline()) {
          <svg lucideDownload class="h-4 w-4"></svg>
          <span i18n="Button that downloads an export straight away"
            >Download</span
          >
        } @else {
          <svg lucidePlay class="h-4 w-4"></svg>
          <span i18n="Button that queues an export to run in the background">
            Start export
          </span>
        }
      </button>

      <button app-stroked-button type="button" (click)="saveDefinition.emit()">
        <span i18n="Button that stores the current export setup for reuse">
          Save as definition
        </span>
      </button>
    </div>
  `,
})
export class ExportReviewStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  readonly preview = input<ExportPreviewResult | null>(null);
  readonly error = input<string | null>(null);
  readonly isBusy = input(false);
  readonly active = input(false);

  readonly startExport = output();
  readonly saveDefinition = output();

  protected readonly canRunInline = computed(() => {
    return this.preview()?.canRunInline ?? false;
  });

  private readonly previewParams = computed(() => {
    const filter = this.wizard.filter();
    const options = this.wizard.options();

    return {
      recordType: this.wizard.recordType(),
      fields: this.wizard.fields(),
      projectKeys: filter.projectKeys,
      boardIdentifiers: filter.boardIdentifiers,
      statusKeys: filter.statusKeys,
      tags: filter.tags,
      term: filter.term ?? undefined,
      dateFormat: options.dateFormat,
      timeZoneId: options.timeZoneId,
      collectionSeparator: options.collectionSeparator,
      expandCollectionsToRows: options.expandCollectionsToRows,
    };
  });

  protected readonly previewData = computed<
    DatatableDataSource<ExportPreviewRow>
  >(() => {
    const byKey = new Map(
      this.wizard.availableFields().map((field) => [field.key, field.name])
    );

    return {
      key: 'export-preview',
      columns: this.wizard.fields().map((fieldKey) => ({
        id: fieldKey,
        header: byKey.get(fieldKey) ?? fieldKey,
        accessor: (row: ExportPreviewRow) => row.values[fieldKey] ?? '',
        cellClass: 'max-w-56 truncate',
      })),
      resource: {
        url: 'api/export/preview/rows',
        params: this.previewParams,
      },
      rows: (response) => response?.payload?.items ?? [],
      trackBy: (_: number, row: ExportPreviewRow) => row.ref,
    };
  });

  protected reviewStats(result: ExportPreviewResult): StatStripItem[] {
    return [
      {
        label: $localize`:Counts the records an export will contain:Records`,
        value: result.estimatedRowCount,
      },
      {
        label: $localize`:Counts the columns an export will contain:Columns`,
        value: result.headers.length,
      },
      {
        label: $localize`:Names the file format an export produces:Format`,
        value: formatName(this.wizard.format()),
      },
    ];
  }
}
