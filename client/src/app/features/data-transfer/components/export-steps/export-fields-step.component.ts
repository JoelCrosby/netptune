import { Component, computed, inject, input } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  FilterFacetComponent,
  FilterFacetToggle,
} from '@static/components/filter-facet/filter-facet.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { FileSizePipe } from '@static/pipes/file-size.pipe';

@Component({
  selector: 'app-export-fields-step',
  imports: [
    FileSizePipe,
    FilterFacetComponent,
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

      <app-filter-facet
        i18n-label="Heading of the optional archive contents list"
        label="Also include"
        [options]="archiveOptions"
        [selected]="archiveSelected()"
        [maxHeight]="'none'"
        (toggled)="onArchiveToggled($event)"
        (cleared)="
          wizard.patchOptions({
            includeMembers: false,
            includeFiles: false,
            includeHistory: false,
          })
        " />

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
        description="Pick the columns this export contains." />

      <app-filter-facet
        i18n-label="Heading of the exported field list"
        label="Fields"
        [options]="fieldOptions()"
        [selected]="wizard.fields()"
        [columns]="2"
        [maxHeight]="'none'"
        i18n-emptyMessage="
          Shown when a record type exposes no exportable fields
        "
        emptyMessage="This record type has no fields to export."
        (toggled)="wizard.toggleField($event.value, $event.selected)"
        (cleared)="wizard.setFields([])">
        <div facetActions class="flex flex-wrap gap-2">
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
      </app-filter-facet>
    }
  `,
})
export class ExportFieldsStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  readonly archiveFileBytes = input(0);

  protected readonly archiveOptions = [
    {
      value: 'members',
      label: $localize`:Archive option that includes workspace members:Members, roles and permissions`,
    },
    {
      value: 'files',
      label: $localize`:Archive option that includes uploaded files:Uploaded files`,
    },
    {
      value: 'history',
      label: $localize`:Archive option that includes the audit history:Audit history`,
    },
  ];

  protected readonly archiveSelected = computed(() => {
    const options = this.wizard.options();
    const included: string[] = [];

    if (options.includeMembers) {
      included.push('members');
    }

    if (options.includeFiles) {
      included.push('files');
    }

    if (options.includeHistory) {
      included.push('history');
    }

    return included;
  });

  protected readonly fieldOptions = computed(() => {
    return this.wizard
      .availableFields()
      .map((field) => ({ value: field.key, label: field.name }));
  });

  protected onArchiveToggled(toggle: FilterFacetToggle) {
    switch (toggle.value) {
      case 'members':
        this.wizard.patchOptions({ includeMembers: toggle.selected });
        return;
      case 'files':
        this.wizard.patchOptions({ includeFiles: toggle.selected });
        return;
      default:
        this.wizard.patchOptions({ includeHistory: toggle.selected });
    }
  }
}
