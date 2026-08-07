import { Component, computed, inject } from '@angular/core';
import {
  ExportFilterListKey,
  ExportWizardService,
} from '@app/features/data-transfer/services/export-wizard.service';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { projectResource } from '@core/resources/project.resource';
import { statusResource } from '@core/resources/status.resources';
import { tagResource } from '@core/resources/tag.resource';
import {
  FilterFacetComponent,
  FilterFacetToggle,
} from '@static/components/filter-facet/filter-facet.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';

@Component({
  selector: 'app-export-filter-step',
  imports: [FilterFacetComponent, FormInputComponent, SectionHeaderComponent],
  template: `
    <app-section-header
      i18n-heading="Heading of the export filter step"
      heading="Filter"
      i18n-description="Explains the export filter step"
      description="Narrow the export down. Leave everything unticked to include it all.">
      @if (activeCount() > 0) {
        <button
          sectionHeaderActions
          type="button"
          class="text-muted hover:text-foreground text-xs transition-colors"
          (click)="wizard.clearFilters()">
          <span i18n="Button that clears every export filter">
            Clear all filters
          </span>
        </button>
      }
    </app-section-header>

    <app-form-input
      class="mb-5 block"
      name="export-search"
      i18n-label="Label of the export search filter"
      label="Search"
      i18n-placeholder="Placeholder of the export search filter"
      placeholder="Match a task name or description"
      [value]="wizard.filter().term ?? ''"
      (valueChange)="onTermChanged($event)" />

    <div class="grid items-start gap-5 md:grid-cols-2">
      <app-filter-facet
        i18n-label="Filter heading for projects"
        label="Projects"
        [options]="projectOptions()"
        [selected]="wizard.filter().projectKeys"
        i18n-emptyMessage="Shown when a workspace has no projects to filter by"
        emptyMessage="This workspace has no projects yet."
        (toggled)="onToggled('projectKeys', $event)"
        (cleared)="wizard.clearFilterList('projectKeys')" />

      <app-filter-facet
        i18n-label="Filter heading for boards"
        label="Boards"
        [options]="boardOptions()"
        [selected]="wizard.filter().boardIdentifiers"
        i18n-emptyMessage="Shown when a workspace has no boards to filter by"
        emptyMessage="This workspace has no boards yet."
        (toggled)="onToggled('boardIdentifiers', $event)"
        (cleared)="wizard.clearFilterList('boardIdentifiers')" />

      <app-filter-facet
        i18n-label="Filter heading for statuses"
        label="Statuses"
        [options]="statusOptions()"
        [selected]="wizard.filter().statusKeys"
        i18n-emptyMessage="Shown when a workspace has no statuses to filter by"
        emptyMessage="This workspace has no statuses yet."
        (toggled)="onToggled('statusKeys', $event)"
        (cleared)="wizard.clearFilterList('statusKeys')" />

      <app-filter-facet
        i18n-label="Filter heading for tags"
        label="Tags"
        [options]="tagOptions()"
        [selected]="wizard.filter().tags"
        i18n-emptyMessage="Shown when a workspace has no tags to filter by"
        emptyMessage="This workspace has no tags yet."
        (toggled)="onToggled('tags', $event)"
        (cleared)="wizard.clearFilterList('tags')" />
    </div>
  `,
})
export class ExportFilterStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  protected readonly projects = projectResource();
  protected readonly boards = workspaceBoardsResource();
  protected readonly statuses = statusResource();
  protected readonly tags = tagResource();

  protected readonly projectOptions = computed(() => {
    return this.projects
      .value()
      .map((project) => ({ value: project.key, label: project.name }));
  });

  protected readonly boardOptions = computed(() => {
    return this.boards
      .value()
      .flatMap((group) => group.boards)
      .map((board) => ({ value: board.identifier, label: board.name }));
  });

  protected readonly statusOptions = computed(() => {
    return this.statuses
      .value()
      .map((status) => ({ value: status.key, label: status.name }));
  });

  protected readonly tagOptions = computed(() => {
    return this.tags
      .value()
      .map((tag) => ({ value: tag.name, label: tag.name }));
  });

  protected readonly activeCount = computed(() => {
    const filter = this.wizard.filter();
    const term = filter.term ? 1 : 0;

    return (
      filter.projectKeys.length +
      filter.boardIdentifiers.length +
      filter.statusKeys.length +
      filter.tags.length +
      term
    );
  });

  protected onToggled(key: ExportFilterListKey, toggle: FilterFacetToggle) {
    this.wizard.toggleFilterValue(key, toggle.value, toggle.selected);
  }

  protected onTermChanged(value: string) {
    this.wizard.patchFilter({ term: value || null });
  }
}
