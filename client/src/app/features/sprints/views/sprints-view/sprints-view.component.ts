import { DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { SprintStatus, sprintStatusLabels } from '@core/enums/sprint-status';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { createSprint, loadSprints } from '@core/store/sprints/sprints.actions';
import {
  selectAllSprints,
  selectSprintCreateLoading,
  selectSprintsLoading,
} from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';

@Component({
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePipe,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    SpinnerComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    CardComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Sprints" />

      @if (loading()) {
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <div class="flex flex-col gap-12">
          @if (canCreateSprints()) {
            <form class="flex flex-col gap-3" (ngSubmit)="onCreateSprint()">
              <h2 class="font-overpass mb-4 text-lg">Create Sprint</h2>

              <app-form-select
                label="Project"
                placeholder="Select project"
                [value]="projectId ?? null"
                (changed)="onProjectSelected($event)">
                @for (project of projects(); track project.id) {
                  <app-form-select-option [value]="project.id!">
                    {{ project.name }}
                  </app-form-select-option>
                }
              </app-form-select>

              <app-form-input
                label="Name"
                name="name"
                [required]="true"
                [(value)]="name" />

              <app-form-textarea
                label="Goal"
                name="goal"
                rows="4"
                [(value)]="goal" />

              <div class="grid grid-cols-2 gap-3">
                <app-form-input
                  label="Start"
                  name="startDate"
                  type="date"
                  [required]="true"
                  [(value)]="startDate" />

                <app-form-input
                  label="End"
                  name="endDate"
                  type="date"
                  [required]="true"
                  [(value)]="endDate" />
              </div>

              <button
                app-flat-button
                color="primary"
                type="submit"
                [disabled]="createLoading() || !projectId">
                Create
              </button>
            </form>
          }

          <div class="flex flex-col gap-3">
            @for (sprint of sprints(); track sprint.id) {
              <div class="bg-board-group p-2">
                <app-card class="min-h-0! gap-3 p-4!">
                  <div class="flex items-start justify-between gap-4">
                    <div>
                      <h2 class="text-xl font-semibold">{{ sprint.name }}</h2>
                      <div class="text-muted text-sm">
                        {{ sprint.projectName }} ·
                        {{ sprint.startDate | date: 'mediumDate' }} -
                        {{ sprint.endDate | date: 'mediumDate' }}
                      </div>
                    </div>

                    <span
                      class="rounded px-2 py-1 text-xs font-semibold"
                      [class.bg-green-100]="
                        sprint.status === sprintStatus.active
                      "
                      [class.text-green-800]="
                        sprint.status === sprintStatus.active
                      "
                      [class.bg-neutral-100]="
                        sprint.status !== sprintStatus.active
                      "
                      [class.text-neutral-700]="
                        sprint.status !== sprintStatus.active
                      ">
                      {{ statusLabel(sprint.status) }}
                    </span>
                  </div>

                  @if (sprint.goal) {
                    <p class="text-sm">{{ sprint.goal }}</p>
                  }

                  <div class="flex items-center justify-between">
                    <span class="text-muted text-sm">
                      {{ sprint.taskCount }} tasks
                    </span>

                    <a
                      app-stroked-button
                      color="primary"
                      [routerLink]="[sprint.id]">
                      Open
                    </a>
                  </div>
                </app-card>
              </div>
            } @empty {
              <app-card class="min-h-0! p-6! text-center">
                No sprints yet.
              </app-card>
            }
          </div>
        </div>
      }
    </app-page-container>
  `,
})
export class SprintsViewComponent {
  private store = inject(Store);

  readonly sprintStatus = SprintStatus;
  readonly loading = this.store.selectSignal(selectSprintsLoading);
  readonly createLoading = this.store.selectSignal(selectSprintCreateLoading);
  readonly sprints = this.store.selectSignal(selectAllSprints);
  readonly projects = this.store.selectSignal(selectAllProjects);
  readonly canCreateSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.create)
  );
  readonly defaultDates = computed(() => {
    const start = new Date();
    const end = new Date();
    end.setDate(start.getDate() + 14);

    return {
      start: start.toISOString().slice(0, 10),
      end: end.toISOString().slice(0, 10),
    };
  });

  projectId?: number;
  name = '';
  goal = '';
  startDate = this.defaultDates().start;
  endDate = this.defaultDates().end;

  constructor() {
    effect(() => {
      const firstProject = this.projects()[0];

      if (!this.projectId && firstProject) {
        this.projectId = firstProject.id;
      }
    });

    this.store.dispatch(loadSprints({}));
  }

  statusLabel(status: SprintStatus) {
    return sprintStatusLabels[status];
  }

  onProjectSelected(projectId: number) {
    this.projectId = projectId;
  }

  onCreateSprint() {
    if (!this.projectId || !this.name.trim()) return;

    this.store.dispatch(
      createSprint({
        request: {
          projectId: this.projectId,
          name: this.name.trim(),
          goal: this.goal.trim() || null,
          startDate: this.startDate,
          endDate: this.endDate,
        },
      })
    );

    this.name = '';
    this.goal = '';
  }
}
