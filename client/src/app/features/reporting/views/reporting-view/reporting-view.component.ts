import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { ReportingUnit } from '@core/models/reporting';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { loadSprints } from '@core/store/sprints/sprints.actions';
import { selectAllSprints } from '@core/store/sprints/sprints.selectors';
import { Store } from '@ngrx/store';
import { CardContentComponent } from '@static/components/card/card-content.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { FlowReportComponent } from '../../components/flow-report.component';
import { SprintReportComponent } from '../../components/sprint-report.component';
import { WorkloadReportComponent } from '../../components/workload-report.component';

const dateValue = (date: Date) => date.toISOString().slice(0, 10);
const today = new Date();
const defaultTo = dateValue(today);
const defaultFrom = dateValue(
  new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000)
);

@Component({
  selector: 'app-reporting-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    CardComponent,
    CardContentComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FlowReportComponent,
    WorkloadReportComponent,
    SprintReportComponent,
  ],
  template: `
    <app-page-container
      [centerPage]="true"
      [marginBottom]="true"
      [fullHeight]="false">
      <app-page-header title="Reports" />

      <app-card class="sticky top-0 z-10 mb-8 block">
        <app-card-header>
          <app-card-title>Report filters</app-card-title>
          <app-card-subtitle>
            Choose the scope, period, and estimation unit used by the reports.
          </app-card-subtitle>
        </app-card-header>

        <app-card-content>
          <div
            class="grid grid-cols-1 gap-x-4 sm:grid-cols-2 lg:grid-cols-5"
            aria-label="Report filters">
            <app-form-select
              class="[&>div]:mb-0!"
              label="Project"
              [value]="projectId() ?? null"
              (valueChange)="setProject($event)">
              <app-form-select-option [value]="null">
                All projects
              </app-form-select-option>
              @for (project of projects(); track project.id) {
                <app-form-select-option [value]="project.id">
                  {{ project.name }}
                </app-form-select-option>
              }
            </app-form-select>

            <app-form-input
              label="From"
              type="date"
              [noMargin]="true"
              [value]="from()"
              (valueChange)="setParam('from', $event)" />

            <app-form-input
              label="To"
              type="date"
              [noMargin]="true"
              [value]="to()"
              (valueChange)="setParam('to', $event)" />

            <app-form-select
              class="[&>div]:mb-0!"
              label="Unit"
              [value]="unit()"
              (valueChange)="setUnit($event)">
              <app-form-select-option value="Tasks">
                Tasks
              </app-form-select-option>
              <app-form-select-option value="StoryPoints">
                Story points
              </app-form-select-option>
              <app-form-select-option value="Hours">
                Hours
              </app-form-select-option>
            </app-form-select>

            <app-form-select
              class="[&>div]:mb-0!"
              label="Sprint"
              [value]="sprintId() ?? null"
              (valueChange)="setSprint($event)">
              <app-form-select-option [value]="null">
                Select sprint
              </app-form-select-option>
              @for (sprint of filteredSprints(); track sprint.id) {
                <app-form-select-option [value]="sprint.id">
                  {{ sprint.name }}
                </app-form-select-option>
              }
            </app-form-select>
          </div>
        </app-card-content>
      </app-card>

      <div class="flex flex-col gap-12">
        <app-flow-report [query]="query()" />
        <app-workload-report [query]="query()" />
        <app-sprint-report
          [projectId]="selectedSprintProjectId() ?? projectId()"
          [sprintId]="sprintId()"
          [unit]="unit()" />
      </div>
    </app-page-container>
  `,
})
export class ReportingViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  readonly projects = this.store.selectSignal(selectAllProjects);
  readonly sprints = this.store.selectSignal(selectAllSprints);
  readonly projectId = computed(() => this.numberParam('projectId'));
  readonly sprintId = computed(() => this.numberParam('sprintId'));
  readonly from = computed(() => this.params().get('from') ?? defaultFrom);
  readonly to = computed(() => this.params().get('to') ?? defaultTo);
  readonly unit = computed<ReportingUnit>(() => {
    const value = this.params().get('unit');
    return value === 'StoryPoints' || value === 'Hours' ? value : 'Tasks';
  });
  readonly filteredSprints = computed(() => {
    const projectId = this.projectId();
    return this.sprints().filter(
      (sprint) => !projectId || sprint.projectId === projectId
    );
  });
  readonly selectedSprintProjectId = computed(
    () =>
      this.sprints().find((sprint) => sprint.id === this.sprintId())?.projectId
  );
  readonly query = computed(() => {
    const values = new URLSearchParams({
      from: this.from(),
      to: this.to(),
      unit: this.unit(),
      timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    });
    const projectId = this.projectId();
    if (projectId) values.set('projectId', String(projectId));
    return values.toString();
  });

  constructor() {
    this.loadProjectOptions();
    this.loadSprintOptions();
  }

  private loadProjectOptions(): void {
    this.store.dispatch(loadProjects.init());
  }

  private loadSprintOptions(): void {
    this.store.dispatch(loadSprints.init({ filter: { take: 100 } }));
  }

  setParam(key: string, value: string): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        [key]: value || null,
        ...(key === 'projectId' ? { sprintId: null } : {}),
      },
      queryParamsHandling: 'merge',
    });
  }

  setProject(projectId: number | null): void {
    this.setParam('projectId', projectId?.toString() ?? '');
  }

  setUnit(unit: ReportingUnit | null): void {
    this.setParam('unit', unit ?? '');
  }

  setSprint(sprintId: number | null): void {
    const sprint = this.sprints().find((item) => item.id === sprintId);
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        sprintId,
        projectId: sprint?.projectId ?? this.projectId() ?? null,
      },
      queryParamsHandling: 'merge',
    });
  }

  private numberParam(key: string): number | undefined {
    const value = Number(this.params().get(key));
    return Number.isInteger(value) && value > 0 ? value : undefined;
  }
}
