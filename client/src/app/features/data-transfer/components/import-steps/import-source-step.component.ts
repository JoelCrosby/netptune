import { Component, inject } from '@angular/core';
import { ImportWizardService } from '@app/features/data-transfer/services/import-wizard.service';
import { ImportSourceProfile } from '@core/models/view-models/import-session';
import { LucideRefreshCw } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import {
  TableComponent,
  TableHeadDirective,
  TableHeaderRowDirective,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-import-source-step',
  imports: [
    LucideRefreshCw,
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    SectionHeaderComponent,
    StatStripComponent,
    StrokedButtonComponent,
    TableComponent,
    TableHeadDirective,
    TableHeaderRowDirective,
    TableRowDirective,
  ],
  template: `
    @if (wizard.profile(); as detected) {
      <app-section-header
        i18n-heading="Heading of the import source step"
        heading="What we found"
        i18n-description="Explains the import source step"
        description="Check this matches your file, then re-read it if you change anything.">
        <button
          sectionHeaderActions
          app-stroked-button
          type="button"
          [disabled]="wizard.isBusy()"
          (click)="wizard.inspect()">
          <svg lucideRefreshCw class="h-4 w-4"></svg>
          <span i18n="Button that re-reads a file with new settings">
            Re-read the file
          </span>
        </button>
      </app-section-header>

      <div class="-mx-6 mb-4">
        <app-stat-strip [items]="sourceStats(detected)" />
      </div>

      <div class="grid gap-x-4 sm:grid-cols-2">
        @if (detected.sheetNames.length > 1) {
          <app-form-select
            name="import-sheet"
            i18n-label="Label of the spreadsheet sheet to import from"
            label="Sheet"
            [value]="wizard.selectedSheet() ?? detected.selectedSheet"
            (changed)="wizard.selectedSheet.set($event ?? null)">
            @for (sheet of detected.sheetNames; track sheet) {
              <app-form-select-option [value]="sheet">
                {{ sheet }}
              </app-form-select-option>
            }
          </app-form-select>
        }

        @if (detected.delimiter) {
          <app-form-input
            name="import-delimiter"
            maxLength="1"
            i18n-label="Label of the character that separates columns"
            label="Delimiter"
            [value]="wizard.delimiterOverride() ?? detected.delimiter"
            (valueChange)="wizard.setDelimiter($event)" />
        }
      </div>

      <app-checkbox
        class="mb-4 block"
        [checked]="wizard.hasHeaderRow()"
        (changed)="wizard.hasHeaderRow.set($event)">
        <span i18n="Option saying the first row holds column names">
          First row holds column names
        </span>
      </app-checkbox>

      <app-table
        class="-mx-6 block"
        containerClass="overflow-x-auto rounded-none border-x-0"
        tableClass="w-full min-w-[640px] text-left text-sm">
        <thead appTableHead>
          <tr appTableHeaderRow>
            <th
              class="px-4 py-3"
              i18n="Column heading for a source column name">
              Column
            </th>
            <th class="px-4 py-3" i18n="Column heading for example values">
              Examples
            </th>
          </tr>
        </thead>
        <tbody>
          @for (column of detected.columns; track column.index) {
            <tr appTableRow>
              <td class="px-4 py-3 font-medium">{{ column.name }}</td>
              <td class="text-muted max-w-96 truncate px-4 py-3">
                {{ column.sampleValues.join(', ') }}
              </td>
            </tr>
          }
        </tbody>
      </app-table>
    } @else {
      <p class="text-muted text-sm" i18n="Shown before a file is uploaded">
        Upload a file to see what it contains.
      </p>
    }
  `,
})
export class ImportSourceStepComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected sourceStats(detected: ImportSourceProfile): StatStripItem[] {
    return [
      {
        label: $localize`:Counts the rows found in an import file:Rows`,
        value: detected.estimatedRowCount,
      },
      {
        label: $localize`:Counts the columns found in an import file:Columns`,
        value: detected.columns.length,
      },
      {
        label: $localize`:Names the character encoding of an import file:Encoding`,
        value: detected.encoding ?? '—',
      },
    ];
  }
}
