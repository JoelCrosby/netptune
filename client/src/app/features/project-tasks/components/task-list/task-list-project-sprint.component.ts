import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { SprintFilterService } from '@core/services/sprint-filter.service';
import { LucideFolder, LucideTimer } from '@lucide/angular';
import { FilterSeparatorComponent } from '@static/components/filter-separator/filter-separator.component';
import {
  SelectFilterComponent,
  SelectFilterOption,
} from '@static/components/select-filter/select-filter.component';

@Component({
  selector: 'app-task-list-project-sprint',
  imports: [FilterSeparatorComponent, SelectFilterComponent],
  host: { class: 'contents' },
  template: `
    @if (canReadProjects()) {
      <app-select-filter
        i18n-label="Label of the project filter"
        label="Project"
        i18n-emptyLabel="Filter option including every project"
        emptyLabel="All projects"
        [icon]="projectIcon"
        [options]="projectOptions()"
        [value]="projectId() ?? null"
        (changed)="setProject($event)" />
    }

    @if (canReadProjects() && canReadSprints()) {
      <app-filter-separator />
    }

    @if (canReadSprints()) {
      <app-select-filter
        i18n-label="Label of the sprint filter"
        label="Sprint"
        i18n-emptyLabel="Filter option including every sprint"
        emptyLabel="All sprints"
        [icon]="sprintIcon"
        [options]="sprintOptions()"
        [value]="sprintId() ?? null"
        (changed)="setSprint($event)" />
    }
  `,
})
export class TaskListProjectSprintComponent {
  private readonly sprintFilter = inject(SprintFilterService);

  protected readonly projectIcon = LucideFolder;
  protected readonly sprintIcon = LucideTimer;

  readonly canReadProjects = hasPermission(PERMISSIONS.projects.read);
  readonly canReadSprints = hasPermission(PERMISSIONS.sprints.read);

  private readonly projects = projectResource().value;
  private readonly sprints = sprintResource([]).value;

  readonly projectId = this.sprintFilter.projectId;
  readonly sprintId = this.sprintFilter.sprintId;

  protected readonly projectOptions = computed<SelectFilterOption<number>[]>(
    () => {
      return this.projects().map((project) => ({
        value: project.id,
        label: project.name,
      }));
    }
  );

  protected readonly sprintOptions = computed<SelectFilterOption<number>[]>(
    () => {
      const projectId = this.projectId();

      return this.sprints()
        .filter((sprint) => !projectId || sprint.projectId === projectId)
        .map((sprint) => ({ value: sprint.id, label: sprint.name }));
    }
  );

  setProject(projectId: number | null) {
    this.sprintFilter.setProject(projectId ?? undefined, this.sprints());
  }

  setSprint(sprintId: number | null) {
    const sprint = this.sprints().find((item) => item.id === sprintId);

    this.sprintFilter.set(sprint);
  }
}
