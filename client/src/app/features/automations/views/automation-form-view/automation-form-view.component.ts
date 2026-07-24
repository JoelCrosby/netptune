import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { automationRuleResource } from '@core/resources/automation.resource';
import { boardGroupOptionsResource } from '@core/resources/board-group.resource';
import { serviceAccountResource } from '@core/resources/service-account.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { statusResource } from '@core/resources/status.resources';
import { tagResource } from '@core/resources/tag.resource';
import { userResource } from '@core/resources/user.resource';
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
  AutomationRule,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
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
                  [users]="workspaceUsers()"
                  [tags]="workspaceTagsResource.value()"
                  [sprints]="workspaceSprintsResource.value()"
                  [boardGroups]="workspaceBoardGroupsResource.value()"
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
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private readonly ruleId = signal(this.readRuleId());

  readonly saving = signal(false);
  readonly validationError = signal<string | null>(null);

  readonly taskStatusesResource = statusResource();
  readonly serviceAccountsResource = serviceAccountResource();
  readonly workspaceUsersResource = userResource();
  readonly workspaceTagsResource = tagResource();
  readonly workspaceSprintsResource = sprintResource([]);
  readonly workspaceBoardGroupsResource = boardGroupOptionsResource();
  readonly ruleResource = automationRuleResource<AutomationRule>(this.ruleId);

  readonly taskStatuses = this.taskStatusesResource.value;
  readonly serviceAccounts = this.serviceAccountsResource.value;

  readonly workspaceUsers = computed(() => {
    return this.workspaceUsersResource.value()?.payload?.items ?? [];
  });

  readonly loading = computed(() => {
    return (
      this.taskStatusesResource.isLoading() ||
      this.serviceAccountsResource.isLoading() ||
      this.workspaceUsersResource.isLoading() ||
      this.workspaceTagsResource.isLoading() ||
      this.workspaceSprintsResource.isLoading() ||
      this.workspaceBoardGroupsResource.isLoading() ||
      this.ruleResource.isLoading()
    );
  });

  readonly enabledServiceAccounts = computed(() => {
    return this.serviceAccounts().filter((account) => !account.disabledAt);
  });

  readonly defaultActiveStatusId = computed(() => {
    return (
      this.statusIdByKey('in-progress') ??
      this.statusIdByKey('active') ??
      this.taskStatuses()[0]?.id ??
      null
    );
  });

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
      const rule = this.ruleResource.value()?.payload;

      if (rule) {
        this.populate(rule);
      }
    });

    effect(() => {
      const ruleLoadError = this.ruleResource.error();

      if (ruleLoadError) {
        this.snackbar.error('Automation could not be loaded');
      }
    });

    effect(() => {
      if (!this.executionUserId()) {
        this.executionUserId.set(
          this.enabledServiceAccounts()[0]?.userId ?? null
        );
      }
    });
  }

  isEdit(): boolean {
    return this.ruleId() !== null;
  }

  cancelLink(): unknown[] {
    return ['../'];
  }

  addAction() {
    if (this.actions().length >= 10) {
      return;
    }

    this.actions.update((actions) => [...actions, this.newNotifyAction()]);
  }

  removeAction(clientId: number) {
    if (this.actions().length === 1) {
      return;
    }

    this.actions.update((actions) => {
      return actions.filter((action) => action.clientId !== clientId);
    });
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
      taskName: null,
      taskDescription: null,
      clearDescription: false,
      ownerId: null,
      clearOwner: false,
      assigneeIds: null,
      addTags: [],
      removeTags: [],
      startDate: null,
      dueDate: null,
      estimateType: null,
      estimateValue: null,
      clearEstimate: false,
      sprintId: null,
      clearSprint: false,
      boardGroupId: null,
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
        conditionGroup: this.conditionGroup(),
        durationDays: null,
      };
    }

    return {
      type: this.triggerType(),
      fields: null,
      durationDays: Number(this.durationDays()),
      conditionGroup: null,
    };
  });

  onSubmit() {
    const request = this.buildRequest();

    if (!request) {
      return;
    }

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

  populate(rule: AutomationRule) {
    const triggerType = rule.trigger.type;
    const taskFields = rule.trigger.fields ?? [];
    const conditionGroup = rule.trigger.conditionGroup ?? null;
    const durationDays = String(rule.trigger.durationDays ?? 3);
    const actions = rule.actions.length
      ? rule.actions.map((action) => ({
          ...action,
          clientId: this.nextActionId++,
        }))
      : [this.newNotifyAction()];

    this.name.set(rule.name);
    this.isEnabled.set(rule.isEnabled);
    this.executionUserId.set(rule.executionUserId);
    this.triggerType.set(triggerType);
    this.taskFields.set(taskFields);
    this.conditionGroup.set(conditionGroup);
    this.durationDays.set(durationDays);
    this.actions.set(actions);
  }

  buildRequest(): AutomationRuleRequest | null {
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

  newNotifyAction(): EditableAutomationAction {
    return {
      clientId: this.nextActionId++,
      type: AutomationActionType.notifyTaskAssignees,
      message: '',
      comment: null,
      flagName: null,
      flagDescription: null,
      statusId: null,
      priority: null,
      taskName: null,
      taskDescription: null,
      clearDescription: false,
      ownerId: null,
      clearOwner: false,
      assigneeIds: null,
      addTags: [],
      removeTags: [],
      startDate: null,
      dueDate: null,
      estimateType: null,
      estimateValue: null,
      clearEstimate: false,
      sprintId: null,
      clearSprint: false,
      boardGroupId: null,
      delayAmount: null,
      delayUnit: null,
    };
  }

  statusIdByKey(key: string): number | null {
    return this.taskStatuses().find((status) => status.key === key)?.id ?? null;
  }

  readRuleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
