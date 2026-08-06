import { Component, inject } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { ExportFormat } from '@core/models/view-models/export-job-view-model';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';

interface FormatOption {
  format: ExportFormat;
  label: string;
  hint: string;
}

@Component({
  selector: 'app-export-format-step',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    SectionHeaderComponent,
    SelectableCardComponent,
  ],
  template: `
    @if (wizard.isArchive()) {
      <app-section-header
        i18n-heading="Heading of the export format step"
        heading="Format"
        i18n-description="Explains that an archive has a fixed format"
        description="An archive is always a .nptz file, so there is nothing to choose here." />
    } @else {
      <app-section-header
        i18n-heading="Heading of the export format step"
        heading="Format"
        i18n-description="Explains the export format step"
        description="Choose the file this export produces." />

      <div class="mb-6 grid gap-2 sm:grid-cols-2 lg:grid-cols-3">
        @for (option of formatOptions; track option.format) {
          <app-selectable-card
            groupName="export-format"
            [accessibleLabel]="option.label"
            [selected]="wizard.format() === option.format"
            (selectionChange)="wizard.format.set(option.format)">
            <span class="min-w-0">
              <span class="block text-sm font-medium">{{ option.label }}</span>
              <span class="text-muted block truncate text-xs">
                {{ option.hint }}
              </span>
            </span>
          </app-selectable-card>
        }
      </div>

      <div class="grid gap-x-4 sm:grid-cols-2">
        <app-form-input
          name="export-delimiter"
          maxLength="1"
          i18n-label="Label of the CSV delimiter option"
          label="Delimiter"
          [value]="wizard.options().delimiter"
          (valueChange)="onDelimiterChanged($event)" />

        <app-form-input
          name="export-date-format"
          i18n-label="Label of the export date format option"
          label="Date format"
          [value]="wizard.options().dateFormat"
          (valueChange)="wizard.patchOptions({ dateFormat: $event })" />

        <app-form-input
          name="export-time-zone"
          i18n-label="Label of the export time zone option"
          label="Time zone"
          [value]="wizard.options().timeZoneId"
          (valueChange)="wizard.patchOptions({ timeZoneId: $event })" />

        <app-form-input
          name="export-collection-separator"
          maxLength="3"
          i18n-label="
            Label of the separator between multiple values in one cell
          "
          label="Multi-value separator"
          [value]="wizard.options().collectionSeparator"
          (valueChange)="
            wizard.patchOptions({ collectionSeparator: $event })
          " />
      </div>

      <div class="flex flex-col gap-3">
        <app-checkbox
          [checked]="wizard.options().includeHeaderRow"
          (changed)="wizard.patchOptions({ includeHeaderRow: $event })">
          <span i18n="Option that writes a header row into the export"
            >Include a header row</span
          >
        </app-checkbox>

        <app-checkbox
          [checked]="wizard.options().expandCollectionsToRows"
          (changed)="wizard.patchOptions({ expandCollectionsToRows: $event })">
          <span i18n="Option that repeats a task once per assignee and tag">
            One row per assignee and tag
          </span>
        </app-checkbox>
      </div>
    }
  `,
})
export class ExportFormatStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  protected readonly formatOptions: FormatOption[] = [
    {
      format: ExportFormat.csv,
      label: $localize`:Comma separated export format:CSV`,
      hint: $localize`:Explains what a CSV export suits:Opens anywhere`,
    },
    {
      format: ExportFormat.tsv,
      label: $localize`:Tab separated export format:TSV`,
      hint: $localize`:Explains what a TSV export suits:Tab separated`,
    },
    {
      format: ExportFormat.xlsx,
      label: $localize`:Excel export format:Excel`,
      hint: $localize`:Explains what an Excel export suits:Typed cells`,
    },
    {
      format: ExportFormat.json,
      label: $localize`:JSON export format:JSON`,
      hint: $localize`:Explains what a JSON export suits:One array of records`,
    },
    {
      format: ExportFormat.ndjson,
      label: $localize`:Newline delimited JSON export format:NDJSON`,
      hint: $localize`:Explains what an NDJSON export suits:One record per line`,
    },
  ];

  protected onDelimiterChanged(value: string) {
    this.wizard.patchOptions({ delimiter: value || ',' });
  }
}
