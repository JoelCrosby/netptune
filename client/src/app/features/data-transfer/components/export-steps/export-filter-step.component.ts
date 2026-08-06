import { Component, computed, inject } from '@angular/core';
import { ExportWizardService } from '@app/features/data-transfer/services/export-wizard.service';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { projectResource } from '@core/resources/project.resource';
import { statusResource } from '@core/resources/status.resources';
import { tagResource } from '@core/resources/tag.resource';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';

@Component({
  selector: 'app-export-filter-step',
  imports: [CheckboxComponent, FormInputComponent, SectionHeaderComponent],
  template: `
    <app-section-header
      i18n-heading="Heading of the export filter step"
      heading="Filter"
      i18n-description="Explains the export filter step"
      description="Narrow the export down. Leave everything unticked to include it all." />

    <div class="grid gap-6 md:grid-cols-2">
      <div>
        <h4 class="mb-2 text-sm font-medium" i18n="Filter heading for projects">
          Projects
        </h4>
        <div class="max-h-44 overflow-auto pr-2">
          @for (project of projects.value(); track project.id) {
            <app-checkbox
              class="mb-2 block"
              [checked]="wizard.hasFilter('projectKeys', project.key)"
              (changed)="
                wizard.toggleFilterValue('projectKeys', project.key, $event)
              ">
              {{ project.name }}
            </app-checkbox>
          }
        </div>
      </div>

      <div>
        <h4 class="mb-2 text-sm font-medium" i18n="Filter heading for boards">
          Boards
        </h4>
        <div class="max-h-44 overflow-auto pr-2">
          @for (board of workspaceBoards(); track board.identifier) {
            <app-checkbox
              class="mb-2 block"
              [checked]="wizard.hasFilter('boardIdentifiers', board.identifier)"
              (changed)="
                wizard.toggleFilterValue(
                  'boardIdentifiers',
                  board.identifier,
                  $event
                )
              ">
              {{ board.name }}
            </app-checkbox>
          }
        </div>
      </div>

      <div>
        <h4 class="mb-2 text-sm font-medium" i18n="Filter heading for statuses">
          Statuses
        </h4>
        <div class="max-h-44 overflow-auto pr-2">
          @for (status of statuses.value(); track status.id) {
            <app-checkbox
              class="mb-2 block"
              [checked]="wizard.hasFilter('statusKeys', status.key)"
              (changed)="
                wizard.toggleFilterValue('statusKeys', status.key, $event)
              ">
              {{ status.name }}
            </app-checkbox>
          }
        </div>
      </div>

      <div>
        <h4 class="mb-2 text-sm font-medium" i18n="Filter heading for tags">
          Tags
        </h4>
        <div class="max-h-44 overflow-auto pr-2">
          @for (tag of tags.value(); track tag.id) {
            <app-checkbox
              class="mb-2 block"
              [checked]="wizard.hasFilter('tags', tag.name)"
              (changed)="wizard.toggleFilterValue('tags', tag.name, $event)">
              {{ tag.name }}
            </app-checkbox>
          }
        </div>
      </div>
    </div>

    <app-form-input
      class="mt-6 block"
      name="export-search"
      i18n-label="Label of the export search filter"
      label="Search"
      i18n-placeholder="Placeholder of the export search filter"
      placeholder="Match a task name or description"
      [value]="wizard.filter().term ?? ''"
      (valueChange)="onTermChanged($event)" />
  `,
})
export class ExportFilterStepComponent {
  protected readonly wizard = inject(ExportWizardService);

  protected readonly projects = projectResource();
  protected readonly boards = workspaceBoardsResource();
  protected readonly statuses = statusResource();
  protected readonly tags = tagResource();

  protected readonly workspaceBoards = computed(() => {
    return this.boards.value().flatMap((group) => group.boards);
  });

  protected onTermChanged(value: string) {
    this.wizard.patchFilter({ term: value || null });
  }
}
