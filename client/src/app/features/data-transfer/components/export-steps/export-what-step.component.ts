import { Component, inject } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { LucideArchive, LucideTable } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { SelectableCardComponent } from '@static/components/selectable-card/selectable-card.component';

@Component({
  selector: 'app-export-what-step',
  imports: [
    LucideArchive,
    LucideTable,
    SectionHeaderComponent,
    SelectableCardComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Heading of the export record type step"
      heading="What to export"
      i18n-description="Explains the first export wizard step"
      description="Choose the records this export contains." />

    <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
      <app-selectable-card
        groupName="export-record-type"
        variant="feature"
        i18n-accessibleLabel="
          Accessible label for the whole-workspace archive card
        "
        accessibleLabel="Export the entire workspace as an archive"
        i18n-heading="Card that exports a whole workspace as an archive"
        heading="Entire workspace"
        i18n-description="Explains what a whole-workspace archive contains"
        description="Portable .nptz archive"
        [selected]="wizard.isArchive()"
        (selectionChange)="wizard.selectArchive()">
        <svg selectableCardIcon lucideArchive class="h-5 w-5"></svg>
      </app-selectable-card>

      @for (
        recordType of wizard.standaloneRecordTypes();
        track recordType.key
      ) {
        <app-selectable-card
          groupName="export-record-type"
          variant="feature"
          [accessibleLabel]="recordType.name"
          [heading]="recordType.name"
          [description]="fieldCountLabel(recordType.fields.length)"
          [selected]="wizard.recordType() === recordType.key"
          (selectionChange)="wizard.selectRecordType(recordType.key)">
          <svg selectableCardIcon lucideTable class="h-5 w-5"></svg>
        </app-selectable-card>
      }
    </div>

    @if (wizard.savedDefinitions.value().length > 0) {
      <app-section-header
        class="mt-8"
        i18n-heading="Heading of the saved export list"
        heading="Saved exports"
        i18n-description="Explains what a saved export does"
        description="Load a setup you have saved before." />

      <div class="flex flex-wrap gap-2">
        @for (saved of wizard.savedDefinitions.value(); track saved.id) {
          <button
            app-stroked-button
            type="button"
            (click)="wizard.loadDefinition(saved)">
            {{ saved.name }}
          </button>
        }
      </div>
    }
  `,
})
export class ExportWhatStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  protected fieldCountLabel(count: number): string {
    return $localize`:Counts the fields a record type can export:${count}:count: fields`;
  }
}
