import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StatusesService } from '@core/services/statuses.service';
import { ServiceAccountsService } from '@core/services/service-accounts.service';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';
import { finalize } from 'rxjs';
import {
  AutomationActionsEditorComponent,
  EditableAutomationAction,
} from '../../components/automation-actions-editor.component';
import { AutomationConditionsEditorComponent } from '../../components/automation-conditions-editor.component';
import { AutomationFormPreviewComponent } from '../../components/automation-form-preview.component';
import { AutomationSettingsEditorComponent } from '../../components/automation-settings-editor.component';
import { AutomationTriggerEditorComponent } from '../../components/automation-trigger-editor.component';
import { buildAutomationRuleRequest } from '../../models/automation-rule-request-builder';
import {
  AutomationActionType,
  AutomationDelayUnit,
  AutomationConditionGroup,
  AutomationConditionGroupOperator,
  AutomationConditionOperator,
  AutomationRule,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
  AssigneeChangeMode,
  TaskChangeField,
} from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  imports: [
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    StepperComponent,
    StepComponent,
    AutomationSettingsEditorComponent,
    AutomationTriggerEditorComponent,
    AutomationConditionsEditorComponent,
    AutomationActionsEditorComponent,
    AutomationFormPreviewComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        [title]="isEdit() ? 'Edit Automation' : 'Create Automation'">
        <a app-stroked-button [routerLink]="cancelLink()">Cancel</a>
        <button
          app-flat-button
          color="primary"
          type="button"
          [disabled]="saving()"
          (click)="onSubmit()">
          {{ isEdit() ? 'Save Automation' : 'Create Automation' }}
        </button>
      </app-page-header>

      @if (loading()) {
        <app-page-loading />
      } @else {
        <form
          class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px]"
          (ngSubmit)="onSubmit()">
          <div class="flex w-full max-w-3xl flex-col gap-5">
            <app-stepper>
              <app-step
                title="Settings"
                description="Name your automation and set whether it is active.">
                <app-automation-settings-editor
                  [serviceAccounts]="enabledServiceAccounts()"
                  [(name)]="name"
                  [(isEnabled)]="isEnabled"
                  [(executionUserId)]="executionUserId" />
              </app-step>

              <app-step
                title="Trigger"
                description="Choose the event that starts this automation.">
                <app-automation-trigger-editor
                  [(triggerType)]="triggerType"
                  [(taskFields)]="taskFields"
                  [(durationDays)]="durationDays" />
              </app-step>

              @if (triggerType() === automationTriggerType.taskChanged) {
                <app-step
                  title="Conditions"
                  description="Optionally restrict which tasks can continue.">
                  <app-automation-conditions-editor
                    [statuses]="taskStatuses()"
                    [(conditionGroup)]="conditionGroup" />
                </app-step>
              }

              <app-step
                title="Actions"
                description="Define what happens when the automation runs.">
                <app-automation-actions-editor
                  [actions]="actions()"
                  [statuses]="taskStatuses()"
                  [defaultStatusId]="defaultActiveStatusId()"
                  (addAction)="addAction()"
                  (removeAction)="removeAction($event)"
                  (actionTypeChanged)="
                    onActionTypeChanged($event.clientId, $event.type)
                  "
                  (actionUpdated)="
                    updateAction($event.clientId, $event.patch)
                  " />
              </app-step>
            </app-stepper>

            @if (validationError()) {
              <p class="text-sm text-red-500">{{ validationError() }}</p>
            }
          </div>

          <app-automation-form-preview
            [trigger]="triggerPreview()"
            [actions]="actions()"
            [statuses]="taskStatuses()" />
        </form>
      }
    </app-page-container>
  `,
})
export class AutomationFormViewComponent {
  readonly automationTriggerType = AutomationTriggerType;

  private service = inject(AutomationsService);
  private statusesService = inject(StatusesService);
  private serviceAccountsService = inject(ServiceAccountsService);
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly validationError = signal<string | null>(null);
  readonly taskStatuses = toSignal(this.statusesService.get(), {
    initialValue: [],
  });
  readonly serviceAccounts = toSignal(this.serviceAccountsService.getAll(), {
    initialValue: [],
  });
  readonly enabledServiceAccounts = computed(() =>
    this.serviceAccounts().filter((account) => !account.disabledAt)
  );
  readonly defaultActiveStatusId = computed(
    () =>
      this.statusIdByKey('in-progress') ??
      this.statusIdByKey('active') ??
      this.taskStatuses()[0]?.id ??
      null
  );

  private nextActionId = 1;

  readonly actions = signal<EditableAutomationAction[]>([
    this.newNotifyAction(),
  ]);

  readonly name = signal('');
  readonly isEnabled = signal(true);
  readonly executionUserId = signal<string | null>(null);
  readonly triggerType = signal(AutomationTriggerType.taskChanged);
  readonly taskFields = signal<TaskChangeField[]>([TaskChangeField.status]);
  readonly conditionGroup = signal<AutomationConditionGroup | null>(null);
  readonly durationDays = signal('3');

  constructor() {
    effect(() => {
      if (!this.executionUserId()) {
        this.executionUserId.set(
          this.enabledServiceAccounts()[0]?.userId ?? null
        );
      }
    });

    if (this.isEdit()) {
      this.loadRule();
    }
  }

  isEdit(): boolean {
    return this.ruleId() !== null;
  }

  cancelLink(): unknown[] {
    return ['../'];
  }

  addAction() {
    if (this.actions().length >= 10) return;
    this.actions.update((actions) => [...actions, this.newNotifyAction()]);
  }

  removeAction(clientId: number) {
    if (this.actions().length === 1) return;
    this.actions.update((actions) =>
      actions.filter((action) => action.clientId !== clientId)
    );
  }

  onActionTypeChanged(clientId: number, type: AutomationActionType) {
    this.updateAction(clientId, {
      type,
      message: type === AutomationActionType.notifyTaskAssignees ? '' : null,
      comment: type === AutomationActionType.addComment ? '' : null,
      flagName: type === AutomationActionType.flagTask ? '' : null,
      flagDescription: type === AutomationActionType.flagTask ? '' : null,
      statusId:
        type === AutomationActionType.updateTask
          ? this.defaultActiveStatusId()
          : null,
      priority: null,
      delayAmount: type === AutomationActionType.deleteTask ? 0 : null,
      delayUnit:
        type === AutomationActionType.deleteTask
          ? AutomationDelayUnit.days
          : null,
    });
  }

  updateAction(clientId: number, patch: Partial<EditableAutomationAction>) {
    this.actions.update((actions) =>
      actions.map((action) =>
        action.clientId === clientId ? { ...action, ...patch } : action
      )
    );
  }

  readonly triggerPreview = computed<AutomationTrigger>(() => {
    if (this.triggerType() === AutomationTriggerType.taskChanged) {
      const fields = [...new Set(this.taskFields())];

      return {
        type: AutomationTriggerType.taskChanged,
        fields,
        conditions: null,
        conditionGroup: this.conditionGroup(),
        statusId: null,
        assigneeChangeMode: null,
        durationDays: null,
      };
    }

    return {
      type: this.triggerType(),
      fields: null,
      durationDays: Number(this.durationDays()),
      conditions: null,
      conditionGroup: null,
      statusId: null,
      assigneeChangeMode: null,
    };
  });

  onSubmit() {
    const request = this.buildRequest();
    if (!request) return;

    const id = this.ruleId();
    const save = id
      ? this.service.update(id, request)
      : this.service.create(request);

    this.saving.set(true);

    save
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (rule) => {
          this.snackbar.open(id ? 'Automation updated' : 'Automation created');
          void this.router.navigate(id ? ['../'] : ['../', rule.id], {
            relativeTo: this.route,
          });
        },
        error: () => this.snackbar.error('Automation could not be saved'),
      });
  }

  private loadRule() {
    const id = this.ruleId();
    if (!id) return;

    this.loading.set(true);
    this.service
      .getRule(id)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (rule) => this.populate(rule),
        error: () => this.snackbar.error('Automation could not be loaded'),
      });
  }

  private populate(rule: AutomationRule) {
    this.name.set(rule.name);
    this.isEnabled.set(rule.isEnabled);
    this.executionUserId.set(rule.executionUserId);
    this.triggerType.set(
      rule.trigger.type === AutomationTriggerType.taskStatusChanged
        ? AutomationTriggerType.taskChanged
        : rule.trigger.type
    );
    this.taskFields.set(
      rule.trigger.fields?.length
        ? rule.trigger.fields
        : [TaskChangeField.status]
    );
    this.conditionGroup.set(this.ruleConditionGroup(rule));
    this.durationDays.set(String(rule.trigger.durationDays ?? 3));
    this.actions.set(
      rule.actions.length
        ? rule.actions.map((action) => ({
            ...action,
            clientId: this.nextActionId++,
          }))
        : [this.newNotifyAction()]
    );
  }

  private buildRequest(): AutomationRuleRequest | null {
    const result = buildAutomationRuleRequest({
      name: this.name(),
      isEnabled: this.isEnabled(),
      executionUserId: this.executionUserId(),
      trigger: this.triggerPreview(),
      actions: this.actions(),
    });

    this.validationError.set(result.error);

    return result.request;
  }

  private newNotifyAction(): EditableAutomationAction {
    return {
      clientId: this.nextActionId++,
      type: AutomationActionType.notifyTaskAssignees,
      message: '',
      comment: null,
      flagName: null,
      flagDescription: null,
      statusId: null,
      priority: null,
      delayAmount: null,
      delayUnit: null,
    };
  }

  private ruleConditionGroup(
    rule: AutomationRule
  ): AutomationConditionGroup | null {
    if (rule.trigger.conditionGroup) {
      return rule.trigger.conditionGroup;
    }

    const conditions = [...(rule.trigger.conditions ?? [])];
    const configuredFields = new Set(
      conditions.map((condition) => condition.field)
    );

    const hasLegacyStatus =
      rule.trigger.statusId !== null && rule.trigger.statusId !== undefined;

    if (hasLegacyStatus && !configuredFields.has(TaskChangeField.status)) {
      conditions.push({
        field: TaskChangeField.status,
        operator: AutomationConditionOperator.equals,
        value: String(rule.trigger.statusId),
      });
      configuredFields.add(TaskChangeField.status);
    }

    const hasLegacyAssigneeMode =
      rule.trigger.assigneeChangeMode !== null &&
      rule.trigger.assigneeChangeMode !== undefined;
    const legacyAssigneeMode = rule.trigger.assigneeChangeMode;

    if (
      hasLegacyAssigneeMode &&
      legacyAssigneeMode !== null &&
      legacyAssigneeMode !== undefined &&
      !configuredFields.has(TaskChangeField.assignees)
    ) {
      conditions.push({
        field: TaskChangeField.assignees,
        operator: this.assigneeOperator(legacyAssigneeMode),
        value: null,
      });
      configuredFields.add(TaskChangeField.assignees);
    }

    for (const field of rule.trigger.fields ?? []) {
      if (!configuredFields.has(field)) {
        conditions.push({
          field,
          operator: AutomationConditionOperator.any,
          value: null,
        });
      }
    }

    if (!conditions.length) return null;

    return {
      operator: AutomationConditionGroupOperator.any,
      conditions,
      groups: [],
    };
  }

  private assigneeOperator(
    mode: AssigneeChangeMode
  ): AutomationConditionOperator {
    switch (mode) {
      case AssigneeChangeMode.added:
        return AutomationConditionOperator.added;
      case AssigneeChangeMode.removed:
        return AutomationConditionOperator.removed;
      default:
        return AutomationConditionOperator.any;
    }
  }

  private statusIdByKey(key: string): number | null {
    return this.taskStatuses().find((status) => status.key === key)?.id ?? null;
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
