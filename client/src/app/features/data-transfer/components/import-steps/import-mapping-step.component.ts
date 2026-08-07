import { Component, inject } from '@angular/core';
import { ImportWizardService } from '@app/features/data-transfer/services/import-wizard.service';
import { LucideSparkles } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  TableComponent,
  TableHeadDirective,
  TableHeaderRowDirective,
  TableRowDirective,
} from '@static/components/table/table.component';

@Component({
  selector: 'app-import-mapping-step',
  imports: [
    FormSelectComponent,
    FormSelectOptionComponent,
    LucideSparkles,
    SectionHeaderComponent,
    StrokedButtonComponent,
    TableComponent,
    TableHeadDirective,
    TableHeaderRowDirective,
    TableRowDirective,
  ],
  template: `
    @if (wizard.profile(); as detected) {
      <app-section-header
        i18n-heading="Heading of the import mapping step"
        heading="Map the columns"
        i18n-description="Explains the import mapping step"
        description="Bind each column in your file to a field. Anything left as Ignore is dropped.">
        @if (wizard.assistantAvailable()) {
          <button
            sectionHeaderActions
            app-stroked-button
            type="button"
            [disabled]="wizard.isBusy()"
            (click)="wizard.improveWithAssistant()">
            <svg lucideSparkles class="h-4 w-4"></svg>
            <span i18n="Button that asks the assistant to improve a mapping">
              Improve with assistant
            </span>
          </button>
        }
      </app-section-header>

      @if (wizard.vendorName(); as recognised) {
        <p class="text-muted mb-4 text-sm">
          <span i18n="Precedes the name of the recognised export tool"
            >Recognised this as a</span
          >
          {{ recognised }}
          <span i18n="Follows the name of the recognised export tool"
            >export and mapped it for you.</span
          >
        </p>
      }

      @if (wizard.assistantAvailable()) {
        <p class="text-muted mb-4 text-xs">
          @if (wizard.allowsDataSampling()) {
            <ng-container
              i18n="Warns that example cell values are sent to the model">
              Column names, types and a few example values are sent to your
              assistant provider.
            </ng-container>
          } @else {
            <ng-container i18n="Says only column names are sent to the model">
              Column names and types only — this workspace does not share
              example values.
            </ng-container>
          }
        </p>

        @if (wizard.assistantNote(); as note) {
          <p class="text-muted mb-4 text-xs">{{ note }}</p>
        }
      }

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
            <th
              class="w-64 px-4 py-3"
              i18n="Column heading for the field a column maps onto">
              Field
            </th>
          </tr>
        </thead>
        <tbody>
          @for (column of detected.columns; track column.index) {
            <tr appTableRow>
              <td class="px-4 py-3 font-medium">{{ column.name }}</td>
              <td class="text-muted max-w-64 truncate px-4 py-3">
                {{ column.sampleValues.join(', ') }}
              </td>
              <td class="px-4 py-3">
                <app-form-select
                  class="[&_.nept-form-control]:mb-0"
                  label=""
                  [name]="'import-field-' + column.index"
                  [value]="wizard.bindingFor(column.index) ?? ''"
                  (changed)="wizard.bindColumn(column.index, $event ?? '')">
                  <app-form-select-option value="">
                    {{ ignoreLabel }}
                  </app-form-select-option>
                  @for (field of wizard.importableFields(); track field.key) {
                    <app-form-select-option [value]="field.key">
                      {{ field.name }}
                    </app-form-select-option>
                  }
                </app-form-select>
              </td>
            </tr>
          }
        </tbody>
      </app-table>

      @if (wizard.mappingError(); as message) {
        <p class="text-warn mt-4 text-sm" role="alert">{{ message }}</p>
      }
    } @else {
      <app-section-header
        i18n-heading="Heading of the import mapping step"
        heading="Map the columns"
        i18n-description="Explains why the mapping step is empty"
        description="Upload a file first — its columns appear here once it has been read." />
    }
  `,
})
export class ImportMappingStepComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected readonly ignoreLabel = $localize`:Mapping option that ignores a source column:Ignore`;
}
