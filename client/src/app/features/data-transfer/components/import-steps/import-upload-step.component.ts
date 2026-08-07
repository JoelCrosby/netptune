import { Component, inject } from '@angular/core';
import { ImportWizardService } from '@app/features/data-transfer/services/import-wizard.service';
import { FileDropzoneComponent } from '@static/components/file-dropzone/file-dropzone.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';

@Component({
  selector: 'app-import-upload-step',
  imports: [
    FileDropzoneComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    SectionHeaderComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Heading of the import upload step"
      heading="Choose a file"
      i18n-description="Explains what an import file may contain"
      description="Upload a CSV, TSV, Excel, JSON or NDJSON file. Up to 50 MB." />

    <app-file-dropzone
      [acceptTypes]="acceptedExtensions"
      (filesSelected)="wizard.setFile($event)" />

    @if (wizard.fileName(); as chosen) {
      <p class="mt-3 text-sm font-medium">{{ chosen }}</p>
    }

    @if (wizard.boards().length > 0) {
      <app-form-select
        class="mt-6 block"
        name="import-board"
        i18n-label="Label of the board an import lands in"
        label="Destination board"
        [value]="wizard.boardIdentifier()"
        (changed)="wizard.boardIdentifier.set($event ?? null)">
        @for (board of wizard.boards(); track board.identifier) {
          <app-form-select-option [value]="board.identifier">
            {{ board.name }}
          </app-form-select-option>
        }
      </app-form-select>
    }
  `,
})
export class ImportUploadStepComponent {
  protected readonly wizard = inject(ImportWizardService);

  protected readonly acceptedExtensions =
    '.csv,.tsv,.txt,.xlsx,.xlsm,.json,.ndjson,.jsonl';
}
