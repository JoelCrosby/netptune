import { Component, inject, input } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';

@Component({
  selector: 'app-export-fields-step',
  imports: [
    CheckboxComponent,
    FileSizePipe,
    SectionHeaderComponent,
    StrokedButtonComponent,
  ],
  template: `
    @if (wizard.isArchive()) {
      <app-section-header
        i18n-heading="Heading of the archive contents step"
        heading="Archive contents"
        i18n-description="Explains what an archive always contains"
        description="An archive always contains every record type. Choose what else to include." />

      <div class="flex flex-col gap-3">
        <app-checkbox
          [checked]="wizard.options().includeMembers"
          (changed)="wizard.patchOptions({ includeMembers: $event })">
          <span i18n="Archive option that includes workspace members">
            Members, roles and permissions
          </span>
        </app-checkbox>

        <app-checkbox
          [checked]="wizard.options().includeFiles"
          (changed)="wizard.patchOptions({ includeFiles: $event })">
          <span i18n="Archive option that includes uploaded files">
            Uploaded files
          </span>
        </app-checkbox>

        <app-checkbox
          [checked]="wizard.options().includeHistory"
          (changed)="wizard.patchOptions({ includeHistory: $event })">
          <span i18n="Archive option that includes the audit history">
            Audit history
          </span>
        </app-checkbox>
      </div>

      @if (archiveFileBytes() > 0 && wizard.options().includeFiles) {
        <p class="text-muted mt-3 text-xs">
          <span i18n="Precedes the size of the files an archive adds"
            >Files add about</span
          >
          {{ archiveFileBytes() | fileSize }}.
        </p>
      }
    } @else {
      <app-section-header
        i18n-heading="Heading of the export field selection step"
        heading="Fields"
        i18n-description="Explains the export field selection step"
        description="Pick the columns this export contains.">
        <div sectionHeaderActions class="flex flex-wrap gap-2">
          <button
            app-stroked-button
            type="button"
            (click)="wizard.selectAllFields()">
            <span i18n="Button that selects every exportable field"
              >Select all</span
            >
          </button>
          <button
            app-stroked-button
            type="button"
            (click)="wizard.selectDefaultFields()">
            <span i18n="Button that restores the default field selection"
              >Defaults only</span
            >
          </button>
        </div>
      </app-section-header>

      <div class="grid gap-2 sm:grid-cols-2">
        @for (field of wizard.availableFields(); track field.key) {
          <app-checkbox
            [checked]="wizard.isFieldSelected(field.key)"
            (changed)="wizard.toggleField(field.key, $event)">
            {{ field.name }}
          </app-checkbox>
        }
      </div>
    }
  `,
})
export class ExportFieldsStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  readonly archiveFileBytes = input(0);
}
