import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { netptunePermissions } from '@core/auth/permissions';
import { ConfirmationService } from '@core/services/confirmation.service';
import { DialogService } from '@core/services/dialog.service';
import { StatusesService } from '@core/services/statuses.service';
import { Status } from '@core/models/status';
import { selectHasPermission } from '@core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';
import { LucidePlus } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { CardComponent } from '@static/components/card/card.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
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
  AutomationRuleSummary,
} from '../../models/automation.models';
import {
  AutomationCloneDialogComponent,
  AutomationCloneDialogData,
  AutomationCloneDialogResult,
} from '../../dialogs/automation-clone-dialog.component';
import { AutomationsService } from '../../services/automations.service';

@Component({
  imports: [
    ErrorStateComponent,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    CardComponent,
    CardHeaderComponent,
    CardSubtitleComponent,
    CardTitleComponent,
    FlatButtonComponent,
    AutomationStatGridComponent,
    AutomationRulesTableComponent,
    LucidePlus,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canManage()) {
        <app-page-header
          title="Automations"
          actionTitle="Create Automation"
          [count]="count()"
          (actionClick)="onCreate()" />
      } @else {
        <app-page-header title="Automations" [count]="count()" />
      }

      @if (loading()) {
        <app-page-loading />
      } @else if (error()) {
        <app-error-state
          title="Automations could not be loaded"
          description="Check your connection and try again."
          (retry)="load()" />
      } @else if (summary()?.ruleCount) {
        <div class="flex flex-col gap-4">
          <app-automation-stat-grid [stats]="stats()" />
          <app-automation-rules-table
            [canManage]="canManage()"
            [statuses]="statuses()"
            [reloadSignal]="reloadToken"
            (toggleRule)="onToggle($event)"
            (editRule)="onEdit($event)"
            (cloneRule)="onClone($event)"
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
  private dialog = inject(DialogService);
  private snackbar = inject(SnackbarService);
  private store = inject(Store);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  readonly summary = signal<AutomationRuleSummary | null>(null);
  readonly statuses = signal<Status[]>([]);
  readonly reloadToken = signal(0);
  readonly loading = signal(true);
  readonly error = signal(false);
  readonly busyId = signal<number | null>(null);
  readonly canManage = this.store.selectSignal(
    selectHasPermission(netptunePermissions.automations.manage)
  );

  readonly count = computed(() =>
    this.loading() ? null : (this.summary()?.ruleCount ?? 0)
  );

  readonly stats = computed<AutomationStat[]>(() => {
    const summary = this.summary();

    return [
      { label: 'Rules', value: summary?.ruleCount ?? 0 },
      { label: 'Enabled', value: summary?.enabledCount ?? 0 },
      { label: 'Recent failures', value: summary?.recentFailureCount ?? 0 },
    ];
  });

  constructor() {
    this.load();
  }

  onCreate() {
    void this.router.navigate(['new'], { relativeTo: this.route });
  }

  reloadRules() {
    this.reloadToken.update((token) => token + 1);
  }

  refresh() {
    this.reloadRules();

    this.service
      .getSummary()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (summary) => this.summary.set(summary),
      });
  }

  load() {
    this.loading.set(true);
    this.error.set(false);

    forkJoin({
      summary: this.service.getSummary(),
      statuses: this.statusesService.get(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: ({ summary, statuses }) => {
          this.summary.set(summary);
          this.statuses.set(statuses);
          this.reloadRules();
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
          this.refresh();
        },
        error: () => this.snackbar.error('Automation could not be updated'),
      });
  }

  onClone(rule: AutomationRuleListItem) {
    const data: AutomationCloneDialogData = {
      ruleName: rule.name,
      trigger: rule.trigger,
      actions: rule.actions,
      statuses: this.statuses(),
    };

    this.dialog
      .open<AutomationCloneDialogResult, AutomationCloneDialogData>(
        AutomationCloneDialogComponent,
        { data }
      )
      .closed.pipe(
        switchMap((result) => {
          if (!result) {
            return EMPTY;
          }

          this.busyId.set(rule.id);

          return this.service.clone(rule.id, result.name);
        }),
        finalize(() => this.busyId.set(null)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (clone) => {
          this.snackbar.open(`Created "${clone.name}" as a disabled copy`);
          void this.router.navigate([clone.id, 'edit'], {
            relativeTo: this.route,
          });
        },
        error: () => this.snackbar.error('Automation could not be cloned'),
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
          this.refresh();
        },
        error: () => this.snackbar.error('Automation could not be deleted'),
      });
  }
}
