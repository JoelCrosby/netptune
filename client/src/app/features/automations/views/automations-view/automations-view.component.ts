import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { StatusesService } from '@core/services/statuses.service';
import { Status } from '@core/models/status';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { EMPTY, finalize, forkJoin, switchMap } from 'rxjs';
import {
  AutomationStat,
  AutomationStatGridComponent,
} from '../../components/automation-stat-grid.component';
import { AutomationRulesTableComponent } from '../../components/automation-rules-table.component';
import {
  AutomationRuleListItem,
  AutomationRunStatus,
} from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    CardComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    AutomationStatGridComponent,
    AutomationRulesTableComponent,
    LucidePlus,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header title="Automations">
        @if (canManage()) {
          <a app-flat-button color="primary" [routerLink]="['new']">
            <svg lucidePlus class="h-4 w-4"></svg>
            Create Automation
          </a>
        }
      </app-page-header>

      @if (loading()) {
        <app-page-loading />
      } @else if (error()) {
        <app-card class="text-center">
          <p class="mb-4 text-sm text-red-500">Failed to load automations.</p>
          <button app-stroked-button type="button" (click)="load()">
            Try Again
          </button>
        </app-card>
      } @else if (rules().length) {
        <div class="flex flex-col gap-4">
          <app-automation-stat-grid [stats]="stats()" />
          <app-automation-rules-table
            [rules]="rules()"
            [canManage]="canManage()"
            [busyId]="busyId()"
            [statuses]="statuses()"
            (toggleRule)="onToggle($event)"
            (editRule)="onEdit($event)"
            (deleteRule)="onDelete($event)" />
        </div>
      } @else {
        <app-card class="text-center">
          <app-card-header>
            <app-card-title>No automations yet</app-card-title>
            <app-card-subtitle>
              Workspace automations can watch task workflow events and apply the
              same follow-up every time.
            </app-card-subtitle>
          </app-card-header>
          @if (canManage()) {
            <a
              app-flat-button
              color="primary"
              class="mt-5"
              [routerLink]="['new']">
              <svg lucidePlus class="h-4 w-4"></svg>
              Create Automation
            </a>
          }
        </app-card>
      }
    </app-page-container>
  `,
})
export class AutomationsViewComponent {
  private service = inject(AutomationsService);
  private statusesService = inject(StatusesService);
  private confirmation = inject(ConfirmationService);
  private snackbar = inject(SnackbarService);
  private store = inject(Store);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  readonly rules = signal<AutomationRuleListItem[]>([]);
  readonly statuses = signal<Status[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly busyId = signal<number | null>(null);
  readonly canManage = this.store.selectSignal(
    selectHasPermission(netptunePermissions.automations.manage)
  );

  readonly enabledCount = computed(
    () => this.rules().filter((rule) => rule.isEnabled).length
  );
  readonly recentFailureCount = computed(
    () =>
      this.rules().filter(
        (rule) => rule.lastRun?.status === AutomationRunStatus.failed
      ).length
  );
  readonly stats = computed<AutomationStat[]>(() => [
    { label: 'Rules', value: this.rules().length },
    { label: 'Enabled', value: this.enabledCount() },
    { label: 'Recent failures', value: this.recentFailureCount() },
  ]);

  constructor() {
    this.load();
  }

  load() {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      rules: this.service.getRulesWithLastRun(),
      statuses: this.statusesService.get(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ rules, statuses }) => {
          this.rules.set(rules);
          this.statuses.set(statuses);
        },
        error: () => this.error.set(true),
      });
  }

  onToggle(rule: AutomationRuleListItem) {
    this.busyId.set(rule.id);
    const request = rule.isEnabled
      ? this.service.disable(rule.id)
      : this.service.enable(rule.id);

    request
      .pipe(
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.open(
            rule.isEnabled ? 'Automation disabled' : 'Automation enabled'
          );
          this.load();
        },
        error: () => this.snackbar.error('Automation could not be updated'),
      });
  }

  onEdit(rule: AutomationRuleListItem) {
    void this.router.navigate([rule.id, 'edit'], { relativeTo: this.route });
  }

  onDelete(rule: AutomationRuleListItem) {
    this.confirmation
      .open({
        title: 'Delete Automation',
        message: `Delete "${rule.name}"? This cannot be undone.`,
        acceptLabel: 'Delete',
        cancelLabel: 'Cancel',
        color: 'warn',
      })
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) {
            return EMPTY;
          }

          this.busyId.set(rule.id);
          return this.service.delete(rule.id);
        }),
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.snackbar.open('Automation deleted');
          this.load();
        },
        error: () => this.snackbar.error('Automation could not be deleted'),
      });
  }
}
