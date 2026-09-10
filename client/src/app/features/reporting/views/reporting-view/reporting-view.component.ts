import { hostTimeZone } from '@core/util/dates';
import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { PERMISSIONS } from '@core/auth/permissions';
import { ReportingGrouping, ReportingUnit } from '@core/models/reporting';
import { projectResource } from '@core/resources/project.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import {
  LucideCalendarRange,
  LucideFolder,
  LucideRuler,
  LucideSlidersHorizontal,
  LucideTimer,
} from '@lucide/angular';
import { DateDropdownButtonComponent } from '@static/components/dropdown-menu/date-dropdown-button.component';
import { FilterSeparatorComponent } from '@static/components/filter-separator/filter-separator.component';
import {
  SelectFilterComponent,
  SelectFilterOption,
} from '@static/components/select-filter/select-filter.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { FlowReportComponent } from '../../components/flow-report.component';
import { SprintReportComponent } from '../../components/sprint-report.component';
import { WorkloadReportComponent } from '../../components/workload-report.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
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
    DateDropdownButtonComponent,
    FilterSeparatorComponent,
    FlowReportComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelHeaderComponent,
    SelectFilterComponent,
    SprintReportComponent,
    WorkloadReportComponent,
  ],
  template: `
    <app-page-container
      followsWidthPreference
      [centerPage]="true"
      [marginBottom]="true"
      [fullHeight]="false">
      <app-page-header
        i18n-title="Page title for the reporting views"
        title="Reports" />

      <section app-panel surface="card" class="sticky top-10 z-12 mb-8">
        <app-panel-header
          density="comfortable"
          [icon]="filterIcon"
          i18n-heading="Heading of the report filter card"
          heading="Report filters"
          i18n-description="Explains what the report filters control"
          description="Choose the scope, period, and estimation unit used by the reports." />

        <app-panel-body>
          <div
            class="flex flex-row flex-wrap items-center gap-3"
            i18n-aria-label="Accessible name of the report filter form"
            aria-label="Report filters">
            <app-select-filter
              i18n-label="Label of the project filter"
              label="Project"
              i18n-emptyLabel="Filter option including every project"
              emptyLabel="All projects"
              [icon]="projectIcon"
              [options]="projectOptions()"
              [value]="projectId() ?? null"
              (changed)="setProject($event)" />

            <app-filter-separator />

            <app-date-dropdown-button
              i18n-label="Label of the start-date filter"
              label="From"
              i18n-ariaLabel="Accessible label for the report start date"
              ariaLabel="Report start date"
              buttonClass="min-w-40 justify-between"
              [value]="from()"
              (valueChanged)="setParam('from', $event)" />

            <app-date-dropdown-button
              i18n-label="Label of the end-date filter"
              label="To"
              i18n-ariaLabel="Accessible label for the report end date"
              ariaLabel="Report end date"
              buttonClass="min-w-40 justify-between"
              [value]="to()"
              (valueChanged)="setParam('to', $event)" />

            <app-filter-separator />

            <app-select-filter
              i18n-label="Label of the estimation unit filter"
              label="Unit"
              i18n-emptyLabel="Estimation unit: whole tasks"
              emptyLabel="Tasks"
              [icon]="unitIcon"
              [options]="unitOptions"
              [value]="unitFilter()"
              (changed)="setUnit($event)" />

            <app-select-filter
              i18n-label="Label of the report grouping filter"
              label="Grouping"
              i18n-emptyLabel="Report grouping by day"
              emptyLabel="Daily"
              [icon]="groupingIcon"
              [options]="groupingOptions"
              [value]="groupingFilter()"
              (changed)="setGrouping($event)" />

            @if (canReadSprints()) {
              <app-filter-separator />

              <app-select-filter
                i18n-label="Label of the sprint filter"
                label="Sprint"
                i18n-emptyLabel="Placeholder option in the sprint filter"
                emptyLabel="Select sprint"
                [icon]="sprintIcon"
                [options]="sprintOptions()"
                [value]="selectedSprintId() ?? null"
                (changed)="setSprint($event)" />
            }
          </div>
        </app-panel-body>
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
  private readonly params = toSignal(this.route.queryParamMap, {
    initialValue: this.route.snapshot.queryParamMap,
  });

  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;
  readonly sprintsResource = sprintResource([]);
  readonly sprints = this.sprintsResource.value;
  readonly canReadMembers = hasPermission(PERMISSIONS.members.read);
  readonly canReadSprints = hasPermission(PERMISSIONS.sprints.read);
  protected readonly projectIcon = LucideFolder;
  protected readonly unitIcon = LucideRuler;
  protected readonly groupingIcon = LucideCalendarRange;
  protected readonly sprintIcon = LucideTimer;

  // The default of each control is the filter's "no filter" entry, so clearing it
  // drops the query parameter and the report falls back to that default.
  protected readonly unitOptions: SelectFilterOption<ReportingUnit>[] = [
    {
      value: 'StoryPoints',
      label: $localize`:Estimation unit of story points:Story points`,
    },
    { value: 'Hours', label: $localize`:Estimation unit of hours:Hours` },
  ];

  protected readonly groupingOptions: SelectFilterOption<ReportingGrouping>[] =
    [{ value: 'Week', label: $localize`:Report grouping by week:Weekly` }];

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
      return this.filteredSprints().map((sprint) => ({
        value: sprint.id,
        label: sprint.name,
      }));
    }
  );

  // Null means "left at the default", which is what the filter's empty entry is.
  protected readonly unitFilter = computed<ReportingUnit | null>(() => {
    const unit = this.unit();

    return unit === 'Tasks' ? null : unit;
  });

  protected readonly groupingFilter = computed<ReportingGrouping | null>(() => {
    const grouping = this.grouping();

    return grouping === 'Day' ? null : grouping;
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
