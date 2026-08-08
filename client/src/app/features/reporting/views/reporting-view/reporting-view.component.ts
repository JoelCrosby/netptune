import { hostTimeZone } from '@core/util/dates';
import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ReportingGrouping, ReportingUnit } from '@core/models/reporting';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { LucideSlidersHorizontal } from '@lucide/angular';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { FlowReportComponent } from '../../components/flow-report.component';
import { SprintReportComponent } from '../../components/sprint-report.component';
import { WorkloadReportComponent } from '../../components/workload-report.component';
import {
  defaultReportingRange,
  defaultReportingSprintId,
  reportingGrouping,
} from '../../utils/reporting-filter-state';

const defaultRange = defaultReportingRange();
const defaultTo = defaultRange.to;
const defaultFrom = defaultRange.from;
@Component({
  selector: 'app-reporting-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    IconTileComponent,
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
      <app-page-header
        i18n-title="Page title for the reporting views"
        title="Reports" />

      <section
        class="border-border bg-card sticky top-10 z-12 mb-8 overflow-hidden rounded-lg border shadow-sm">
        <header class="border-border border-b px-6 py-5">
          <div class="flex min-w-0 items-center gap-3">
            <app-icon-tile [icon]="filterIcon" />

            <div class="min-w-0">
              <h2
                class="font-overpass text-base font-semibold"
                i18n="Heading of the report filter card">
                Report filters
              </h2>
              <p
                class="text-muted mt-1 text-sm"
                i18n="Explains what the report filters control">
                Choose the scope, period, and estimation unit used by the
                reports.
              </p>
            </div>
          </div>
        </header>

        <div class="px-6 py-5">
          <div
            class="grid grid-cols-1 gap-x-4 sm:grid-cols-2 lg:grid-cols-6"
            i18n-aria-label="Accessible name of the report filter form"
            aria-label="Report filters">
            <app-form-select
              class="[&>div]:mb-0!"
              i18n-label="Label of the project filter"
              label="Project"
              [value]="projectId() ?? null"
              (valueChange)="setProject($event)">
              <app-form-select-option [value]="null">
                <span i18n="Filter option including every project">
                  All projects
                </span>
              </app-form-select-option>
              @for (project of projects(); track project.id) {
                <app-form-select-option [value]="project.id">
                  {{ project.name }}
                </app-form-select-option>
              }
            </app-form-select>

            <app-form-input
              i18n-label="Label of the start-date filter"
              label="From"
              type="date"
              [noMargin]="true"
              [value]="from()"
              (valueChange)="setParam('from', $event)" />

            <app-form-input
              i18n-label="Label of the end-date filter"
              label="To"
              type="date"
              [noMargin]="true"
              [value]="to()"
              (valueChange)="setParam('to', $event)" />

            <app-form-select
              class="[&>div]:mb-0!"
              i18n-label="Label of the estimation unit filter"
              label="Unit"
              [value]="unit()"
              (valueChange)="setUnit($event)">
              <app-form-select-option value="Tasks">
                <span i18n="Estimation unit: whole tasks">Tasks</span>
              </app-form-select-option>
              <app-form-select-option value="StoryPoints">
                <span i18n="Estimation unit: story points">Story points</span>
              </app-form-select-option>
              <app-form-select-option value="Hours">
                <span i18n="Estimation unit: hours">Hours</span>
              </app-form-select-option>
            </app-form-select>

            <app-form-select
              class="[&>div]:mb-0!"
              i18n-label="Label of the report grouping filter"
              label="Grouping"
              [value]="grouping()"
              (valueChange)="setGrouping($event)">
              <app-form-select-option value="Day">
                <span i18n="Report grouping by day">Daily</span>
              </app-form-select-option>
              <app-form-select-option value="Week">
                <span i18n="Report grouping by week">Weekly</span>
              </app-form-select-option>
            </app-form-select>

            @if (canReadSprints()) {
              <app-form-select
                class="[&>div]:mb-0!"
                i18n-label="Label of the sprint filter"
                label="Sprint"
                [value]="selectedSprintId() ?? null"
                (valueChange)="setSprint($event)">
                <app-form-select-option [value]="null">
                  <span i18n="Placeholder option in the sprint filter">
                    Select sprint
                  </span>
                </app-form-select-option>
                @for (sprint of filteredSprints(); track sprint.id) {
                  <app-form-select-option [value]="sprint.id">
                    {{ sprint.name }}
                  </app-form-select-option>
                }
              </app-form-select>
            }
          </div>
        </div>
      </section>

      <div class="flex flex-col gap-12">
        <app-flow-report [query]="query()" />
        @if (canReadMembers()) {
          <app-workload-report [query]="query()" />
        }
        @if (canReadSprints()) {
          <app-sprint-report
            [projectId]="selectedSprintProjectId() ?? projectId()"
            [sprintId]="selectedSprintId()"
            [timeZone]="timeZone()"
            [unit]="unit()" />
        }
      </div>
    </app-page-container>
  `,
})
export class ReportingViewComponent {
  protected readonly filterIcon = LucideSlidersHorizontal;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(Store);
  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;
  readonly sprintsResource = sprintResource([]);
  readonly sprints = this.sprintsResource.value;
  readonly canReadMembers = this.store.selectSignal(
    selectHasPermission(netptunePermissions.members.read)
  );
  readonly canReadSprints = this.store.selectSignal(
    selectHasPermission(netptunePermissions.sprints.read)
  );
  readonly projectId = computed(() => this.numberParam('projectId'));
  readonly sprintId = computed(() => this.numberParam('sprintId'));
  readonly from = computed(() => this.params().get('from') ?? defaultFrom);
  readonly to = computed(() => this.params().get('to') ?? defaultTo);
  readonly timeZone = computed(
    () => this.params().get('timeZone') ?? hostTimeZone()
  );
  readonly grouping = computed<ReportingGrouping>(() =>
    reportingGrouping(this.params().get('grouping'))
  );
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
  readonly selectedSprintId = computed(() => {
    const requestedSprintId = this.sprintId();

    if (requestedSprintId) {
      return requestedSprintId;
    }

    return defaultReportingSprintId(this.sprints(), this.projectId());
  });
  readonly selectedSprintProjectId = computed(
    () =>
      this.sprints().find((sprint) => sprint.id === this.selectedSprintId())
        ?.projectId
  );
  readonly query = computed(() => {
    const values = new URLSearchParams({
      from: this.from(),
      to: this.to(),
      unit: this.unit(),
      timeZone: this.timeZone(),
      grouping: this.grouping(),
    });
    const projectId = this.projectId();
    if (projectId) values.set('projectId', String(projectId));
    return values.toString();
  });

  constructor() {
    this.loadSprintOptions();
    this.ensureDefaultParams();
  }

  private loadSprintOptions(): void {
    const canLoadSprintOptions = this.canReadSprints();

    if (!canLoadSprintOptions) {
      return;
    }
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

  setGrouping(grouping: ReportingGrouping | null): void {
    this.setParam('grouping', grouping ?? '');
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

  private ensureDefaultParams(): void {
    const queryParams = this.route.snapshot.queryParamMap;
    const hasDateRange = queryParams.has('from') && queryParams.has('to');
    const hasUnit = queryParams.has('unit');
    const hasTimeZone = queryParams.has('timeZone');
    const hasGrouping = queryParams.has('grouping');
    const hasCompleteFilterState =
      hasDateRange && hasUnit && hasTimeZone && hasGrouping;

    if (hasCompleteFilterState) {
      return;
    }

    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {
        from: queryParams.get('from') ?? defaultFrom,
        to: queryParams.get('to') ?? defaultTo,
        unit: queryParams.get('unit') ?? 'Tasks',
        timeZone: queryParams.get('timeZone') ?? hostTimeZone(),
        grouping: queryParams.get('grouping') ?? 'Day',
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
