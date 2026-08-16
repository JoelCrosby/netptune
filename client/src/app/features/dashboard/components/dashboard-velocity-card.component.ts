import { httpResource } from '@angular/common/http';
import { Component, computed, linkedSignal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { VelocityReport } from '@core/models/reporting';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { projectResource } from '@core/resources/project.resource';
import { LucideGauge } from '@lucide/angular';
import { ChartCardComponent } from '@static/components/chart-card/chart-card.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import {
  StatStripComponent,
  StatStripItem,
} from '@static/components/stat-strip/stat-strip.component';
import { VelocityChartComponent } from './charts/velocity-chart.component';

const recentSprints = 8;

@Component({
  selector: 'app-dashboard-velocity-card',
  imports: [
    ChartCardComponent,
    EmptyStateComponent,
    SelectMenuComponent,
    SkeletonComponent,
    StatStripComponent,
    VelocityChartComponent,
    LucideGauge,
  ],
  host: { class: 'block h-full' },
  template: `
    <app-chart-card
      [icon]="velocityIcon"
      i18n-title="Heading of the dashboard velocity card"
      title="Sprint velocity"
      i18n-description="Explains what the velocity chart compares"
      description="Committed against completed, by sprint">
      <span chartCardActions class="contents">
        @if (projectOptions().length > 1) {
          <app-select-menu
            [options]="projectOptions()"
            [value]="projectId()"
            i18n-ariaLabel="Accessible label for the velocity project picker"
            ariaLabel="Choose a project"
            buttonClass="border-border text-foreground hover:bg-foreground/5 h-9 max-w-48 min-w-0 gap-2 rounded-md border bg-transparent px-3 text-sm font-normal tracking-normal"
            (valueChange)="projectId.set($event)" />
        }
      </span>

      @if (isInitialLoad()) {
        <div
          role="status"
          i18n-aria-label="Accessible label while the velocity chart loads"
          aria-label="Loading sprint velocity">
          <app-skeleton class="h-52 w-full" />
          <app-skeleton class="mt-6 h-10 w-full" />
        </div>
      } @else if (sprints().length) {
        <app-velocity-chart [sprints]="sprints()" />
        <div class="-mx-6 mt-5 -mb-5">
          <app-stat-strip [items]="stats()" />
        </div>
      } @else {
        <app-empty-state
          compact
          i18n-title="Empty state for the dashboard velocity card"
          title="No completed sprints yet."
          i18n-description="Explains why the velocity chart is empty"
          description="Velocity appears once a sprint has been completed.">
          <svg emptyStateIcon lucideGauge class="h-8 w-8"></svg>
        </app-empty-state>
      }
    </app-chart-card>
  `,
})
export class DashboardVelocityCardComponent {
  protected readonly velocityIcon = LucideGauge;

  readonly canRead = hasPermission(PERMISSIONS.sprints.read);

  private readonly projectsResource = projectResource();
  private readonly projects = this.projectsResource.value;

  /**
   * Defaults to the first project once the list arrives, but keeps whatever the
   * viewer picked for as long as that project is still in the list.
   */
  protected readonly projectId = linkedSignal<
    ProjectViewModel[],
    number | undefined
  >({
    source: this.projects,
    computation: (projects, previous) => {
      const stillListed = projects.some(
        (project) => project.id === previous?.value
      );

      return stillListed ? previous?.value : projects[0]?.id;
    },
  });

  protected readonly projectOptions = computed<
    SelectMenuOption<number | undefined>[]
  >(() =>
    this.projects().map((project) => ({
      label: project.name,
      value: project.id,
    }))
  );

  private readonly resource = httpResource<VelocityReport>(() => {
    const projectId = this.projectId();

    if (!projectId || !this.canRead()) return undefined;

    return {
      url: 'api/reports/velocity',
      params: { projectId, unit: 'Tasks', take: recentSprints },
    };
  });

  protected readonly isInitialLoad = computed(
    () => this.resource.isLoading() && !this.resource.hasValue()
  );

  protected readonly sprints = computed(
    () => this.resource.value()?.sprints ?? []
  );

  protected readonly stats = computed<StatStripItem[]>(() => {
    const sprints = this.sprints();

    if (!sprints.length) return [];

    const completed = sprints.reduce((sum, point) => sum + point.completed, 0);
    const committed = sprints.reduce((sum, point) => sum + point.committed, 0);
    const average = completed / sprints.length;
    const hitRate = committed ? Math.round((completed / committed) * 100) : 0;

    return [
      {
        label: $localize`:Label for the mean work completed per sprint:Average velocity`,
        value: Math.round(average * 10) / 10,
      },
      {
        label: $localize`:Label for the best sprint result:Best sprint`,
        value: Math.max(...sprints.map((point) => point.completed)),
      },
      {
        label: $localize`:Label for the share of committed work that was finished:Commitment met`,
        value: `${hitRate}%`,
      },
    ];
  });
}
