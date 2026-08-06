import { Component, inject } from '@angular/core';
import { ImportWizardService } from '@app/features/data-transfer/services/import-wizard.service';
import { ImportPreviewResult } from '@core/models/view-models/import-session';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
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
  selector: 'app-import-preview-step',
  imports: [
    CheckboxComponent,
    FlatButtonComponent,
    SectionHeaderComponent,
    StatStripComponent,
    StrokedButtonComponent,
    TableComponent,
    TableHeadDirective,
    TableHeaderRowDirective,
    TableRowDirective,
  ],
  template: `
    <app-section-header
      i18n-heading="Heading of the import preview step"
      heading="What will change"
      i18n-description="Explains the import preview step"
      description="A dry run over your file. Nothing is written until you commit." />

    @if (wizard.preview(); as result) {
      <div class="-mx-6 mb-4">
        <app-stat-strip [items]="previewStats(result)" />
      </div>

      @if (result.newEntities.length > 0) {
        <p class="text-muted mb-3 text-sm">
          <span i18n="Precedes the list of things an import will create"
            >This import will also create</span
          >
          {{ wizard.newEntityLabel() }}.
        </p>
      }

      @if (result.usersToInvite.length > 0) {
        <p class="text-muted mb-3 text-sm">
          <span i18n="Precedes emails that do not belong to members"
            >Not workspace members</span
          >: {{ result.usersToInvite.join(', ') }}
        </p>
      }

      @if (wizard.groupedDiagnostics().length > 0) {
        <app-table
          class="-mx-6 mb-4 block"
          containerClass="overflow-x-auto rounded-none border-x-0"
          tableClass="w-full min-w-[480px] text-left text-sm">
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th
                class="px-4 py-3"
                i18n="Column heading for what went wrong with a row">
                Problem
              </th>
              <th
                class="w-28 px-4 py-3 text-right"
                i18n="Column heading for how many rows share a problem">
                Rows
              </th>
            </tr>
          </thead>
          <tbody>
            @for (group of wizard.groupedDiagnostics(); track group.code) {
              <tr appTableRow>
                <td class="px-4 py-3">{{ group.message }}</td>
                <td class="text-muted px-4 py-3 text-right">
                  {{ group.count }}
                </td>
              </tr>
            }
          </tbody>
        </app-table>
      }

      @if (result.willError > 0) {
        <app-checkbox
          [checked]="wizard.skipFailingRows()"
          (changed)="wizard.skipFailingRows.set($event)">
          <span i18n="Option that imports everything except failing rows">
            Skip rows that cannot be imported
          </span>
        </app-checkbox>
      }

      <button
        app-flat-button
        class="mt-6"
        type="button"
        [disabled]="!wizard.canCommit() || wizard.isBusy()"
        (click)="wizard.commit()">
        <span i18n="Button that starts an import">Import</span>
      </button>
    } @else {
      <button
        app-stroked-button
        type="button"
        [disabled]="wizard.isBusy()"
        (click)="wizard.loadPreview()">
        <span i18n="Button that builds an import preview">
          Build a preview
        </span>
      </button>
    }
  `,
})
export class ImportPreviewStepComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected previewStats(result: ImportPreviewResult): StatStripItem[] {
    return [
      {
        label: $localize`:Counts records an import would create:Create`,
        value: result.willCreate,
      },
      {
        label: $localize`:Counts records an import would update:Update`,
        value: result.willUpdate,
      },
      {
        label: $localize`:Counts records an import would skip:Skip`,
        value: result.willSkip,
      },
      {
        label: $localize`:Counts rows an import cannot use:Error`,
        value: result.willError,
      },
    ];
  }
}
