import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  injectAsync,
  onIdle,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { automationRuleResource } from '@core/resources/automation.resource';
import { boardGroupOptionsResource } from '@core/resources/board-group.resource';
import { workspaceBoardsResource } from '@core/resources/board.resource';
import { projectResource } from '@core/resources/project.resource';
import { relationTypeResource } from '@core/resources/relation-type.resource';
import { serviceAccountResource } from '@core/resources/service-account.resource';
import { sprintResource } from '@core/resources/sprint.resource';
import { statusResource } from '@core/resources/status.resource';
import { tagResource } from '@core/resources/tag.resource';
import { userResource } from '@core/resources/user.resource';
import {
  LucideCircleAlert,
  LucideListFilter,
  LucideZap,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { FormControlShapeDirective } from '@static/components/form-control/form-control.directives';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { finalize } from 'rxjs';
import {
  AutomationActionsEditorComponent,
  EditableAutomationAction,
  automationActionLimit,
} from '../../components/automation-actions-editor.component';
import { AutomationConditionsEditorComponent } from '../../components/automation-conditions-editor.component';
import { AutomationFlowStepComponent } from '../../components/automation-flow-step.component';
import { AutomationSettingsEditorComponent } from '../../components/automation-settings-editor.component';
import { AutomationSetupStripComponent } from '../../components/automation-setup-strip.component';
import { AutomationSummaryBarComponent } from '../../components/automation-summary-bar.component';
import { AutomationTriggerEditorComponent } from '../../components/automation-trigger-editor.component';
import {
  describeAutomationOneLine,
  scopeKindLabels,
} from '../../models/automation-copy';
import {
  AutomationActionType,
  AutomationDelayUnit,
  AutomationNotificationRecipient,
  AutomationRelationDirection,
  AutomationRelationOperation,
  AutomationConditionGroup,
  AutomationRule,
  AutomationRuleRequest,
  AutomationTrigger,
  AutomationTriggerType,
  TaskChangeField,
} from '../../models/automation.models';
import { AutomationsService } from '../../services/automations.service';

@Component({
  selector: 'app-automation-form-view',
  imports: [
    RouterLink,
    CalloutComponent,
    FormControlShapeDirective,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    PageLoadingComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    AutomationFlowStepComponent,
    AutomationSettingsEditorComponent,
    AutomationSetupStripComponent,
    AutomationSummaryBarComponent,
    AutomationTriggerEditorComponent,
    AutomationConditionsEditorComponent,
    AutomationActionsEditorComponent,
  ],
  template: `
    <app-page-container layout="list" [stickyFooter]="true">
      <app-page-header
        toolbar
        [title]="isEdit() ? 'Edit Automation' : 'Create Automation'" />

      <app-page-body scroll>
        @if (loading()) {
          <app-page-loading />
        } @else {
          <form
            appFormShape="rounded"
            class="mx-auto flex w-full flex-col gap-4 pb-8"
            (ngSubmit)="onSubmit()">
            <app-automation-setup-strip
              [name]="name()"
              [runAs]="runAsLabel()"
              [scope]="scopeLabel()"
              [isEnabled]="isEnabled()"
              [(open)]="setupOpen">
              <app-automation-settings-editor
                [serviceAccounts]="enabledServiceAccounts()"
                [projects]="projectsResource.value()"
                [boards]="workspaceBoards()"
                [sprints]="workspaceSprintsResource.value()"
                [(name)]="name"
                [(isEnabled)]="isEnabled"
                [(executionUserId)]="executionUserId"
                [(projectId)]="projectId"
                [(boardId)]="boardId"
                [(sprintId)]="sprintId" />
            </app-automation-setup-strip>

            <app-automation-summary-bar
              [trigger]="triggerPreview()"
              [actions]="actions()"
              [statuses]="taskStatuses()"
              [(open)]="summaryOpen" />

            <div class="flex flex-col">
              <app-automation-flow-step [icon]="triggerIcon">
                <app-automation-trigger-editor
                  [(triggerType)]="triggerType"
                  [(taskFields)]="taskFields"
                  [(durationDays)]="durationDays" />
              </app-automation-flow-step>

              <app-automation-flow-step
                appearance="outline"
                [icon]="conditionsIcon">
                <app-automation-conditions-editor
                  [statuses]="taskStatuses()"
                  [supportsChangeOperators]="
                    triggerType() === automationTriggerType.taskChanged
                  "
                  [(conditionGroup)]="conditionGroup" />
              </app-automation-flow-step>

              <app-automation-actions-editor
                [actions]="actions()"
                [statuses]="taskStatuses()"
                [users]="workspaceUsers()"
                [ruleName]="name()"
                [tags]="workspaceTagsResource.value()"
                [sprints]="workspaceSprintsResource.value()"
                [boardGroups]="workspaceBoardGroupsResource.value()"
                [relationTypes]="relationTypesResource.value()"
                [defaultStatusId]="defaultActiveStatusId()"
                (addAction)="addAction()"
                (removeAction)="removeAction($event)"
                (actionTypeChanged)="
                  onActionTypeChanged($event.clientId, $event.type)
                "
                (actionUpdated)="updateAction($event.clientId, $event.patch)" />
            </div>
          </form>
        }
      </app-page-body>

      @if (!loading()) {
        <div
          pageFooter
          class="mx-auto flex w-full max-w-265 flex-col gap-3 py-4">
          @if (validationError(); as error) {
            <app-callout color="warn" role="alert" [icon]="errorIcon">
              {{ error }}
            </app-callout>
          }

          <div class="flex items-center gap-4">
            <p
              class="text-foreground/60 min-w-0 flex-1 text-[13px] text-pretty">
              {{ oneLine() }}
            </p>

            <a
              app-stroked-button
              [routerLink]="cancelLink()"
              i18n="Dismisses a dialog without acting">
              Cancel
            </a>

            <button
              app-flat-button
              color="primary"
              type="button"
              [disabled]="saving()"
              (click)="onSubmit()">
              {{ isEdit() ? 'Save Automation' : 'Create Automation' }}
            </button>
          </div>
        </div>
      }
    </app-page-container>
  `,
})
export class AutomationFormViewComponent {
  readonly automationTriggerType = AutomationTriggerType;
  readonly triggerIcon = LucideZap;
  readonly conditionsIcon = LucideListFilter;
  readonly errorIcon = LucideCircleAlert;

  private service = inject(AutomationsService);
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private requestBuilder = injectAsync(
    () =>
      import('../../services/automation-rule-request-builder.service').then(
        (m) => m.AutomationRuleRequestBuilder
      ),
    { prefetch: onIdle }
  );
  private readonly ruleId = signal(this.readRuleId());

  readonly saving = signal(false);
  readonly validationError = signal<string | null>(null);
  readonly setupOpen = signal(false);
  readonly summaryOpen = signal(true);

  readonly taskStatusesResource = statusResource();
  readonly serviceAccountsResource = serviceAccountResource();
  readonly workspaceUsersResource = userResource();
  readonly workspaceTagsResource = tagResource();
  readonly workspaceSprintsResource = sprintResource([]);
  readonly workspaceBoardGroupsResource = boardGroupOptionsResource();
  readonly relationTypesResource = relationTypeResource();
  readonly projectsResource = projectResource();
  readonly workspaceBoardsResource = workspaceBoardsResource();

  readonly workspaceBoards = computed(() => {
    return this.workspaceBoardsResource
      .value()
      .flatMap((project) => project.boards);
  });
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
  readonly projectId = signal<number | null>(null);
  readonly boardId = signal<number | null>(null);
  readonly sprintId = signal<number | null>(null);

  readonly runAsLabel = computed(() => {
    const account = this.enabledServiceAccounts().find(
      (candidate) => candidate.userId === this.executionUserId()
    );

    return (
      account?.name ??
      $localize`:Stands in for the service account an automation has not been given yet:no service account`
    );
  });

  readonly scopeLabel = computed(() => {
    const projectId = this.projectId();

    if (projectId !== null) {
      const project = this.projectsResource
        .value()
        .find((candidate) => candidate.id === projectId);

      return this.scopedLabel('project', project?.name);
    }

    const boardId = this.boardId();

    if (boardId !== null) {
      const board = this.workspaceBoards().find(
        (candidate) => candidate.id === boardId
      );

      return this.scopedLabel('board', board?.name);
    }

    const sprintId = this.sprintId();

    if (sprintId !== null) {
      const sprint = this.workspaceSprintsResource
        .value()
        .find((candidate) => candidate.id === sprintId);

      return this.scopedLabel('sprint', sprint?.name);
    }

    return $localize`:Scope of an automation that covers every task in the workspace:Whole workspace`;
  });

  readonly oneLine = computed(() => {
    return describeAutomationOneLine(this.triggerPreview(), this.actions());
  });

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
        this.snackbar.error(
          $localize`:Error after failing to load an automation:Automation could not be loaded`
        );
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
    if (this.actions().length >= automationActionLimit) {
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
      recipients:
        type === AutomationActionType.notifyTaskAssignees
          ? [AutomationNotificationRecipient.assignees]
          : [],
      recipientUserIds: [],
      recipientRoles: [],
      comment: type === AutomationActionType.addComment ? '' : null,
      flagName: type === AutomationActionType.flagTask ? '' : null,
      flagDescription: type === AutomationActionType.flagTask ? '' : null,
      statusId:
        type === AutomationActionType.updateTask
          ? this.defaultActiveStatusId()
          : null,
      priority: null,
      taskName: type === AutomationActionType.createTask ? '' : null,
      taskDescription: null,
      clearDescription: false,
      ownerId: null,
      clearOwner: false,
      assigneeIds: type === AutomationActionType.createTask ? [] : null,
      copyAssignees: false,
      linkRelationTypeId: null,
      relationOperation:
        type === AutomationActionType.manageTaskRelation
          ? AutomationRelationOperation.add
          : null,
      relationDirection:
        type === AutomationActionType.manageTaskRelation
          ? AutomationRelationDirection.taskIsSource
          : null,
      relationTypeId: null,
      relatedTaskId: null,
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

    const usesDuration =
      this.triggerType() === AutomationTriggerType.taskUnassignedFor ||
      this.triggerType() === AutomationTriggerType.taskDueDateApproaching ||
      this.triggerType() === AutomationTriggerType.taskInactiveFor ||
      this.triggerType() === AutomationTriggerType.sprintEndingSoon;

    return {
      type: this.triggerType(),
      fields: null,
      durationDays: usesDuration ? Number(this.durationDays()) : null,
      conditionGroup: this.conditionGroup(),
    };
  });

  async onSubmit() {
    const request = await this.buildRequest();

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
        error: () =>
          this.snackbar.error(
            $localize`:Error after failing to save an automation:Automation could not be saved`
          ),
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
    this.projectId.set(rule.projectId ?? null);
    this.boardId.set(rule.boardId ?? null);
    this.sprintId.set(rule.sprintId ?? null);
  }

  async buildRequest(): Promise<AutomationRuleRequest | null> {
    const builder = await this.requestBuilder();
    const result = builder.build({
      name: this.name(),
      isEnabled: this.isEnabled(),
      executionUserId: this.executionUserId(),
      trigger: this.triggerPreview(),
      actions: this.actions(),
      projectId: this.projectId(),
      boardId: this.boardId(),
      sprintId: this.sprintId(),
    });

    this.validationError.set(result.error);

    // The setup fields collapse into a summary line, so a failure there would otherwise be invisible.
    if (result.errorStep === 'settings') {
      this.setupOpen.set(true);
    }

    return result.request;
  }

  newNotifyAction(): EditableAutomationAction {
    return {
      clientId: this.nextActionId++,
      type: AutomationActionType.notifyTaskAssignees,
      message: '',
      recipients: [AutomationNotificationRecipient.assignees],
      recipientUserIds: [],
      recipientRoles: [],
      copyAssignees: false,
      linkRelationTypeId: null,
      relationOperation: null,
      relationDirection: null,
      relationTypeId: null,
      relatedTaskId: null,
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

  private scopedLabel(
    kind: 'project' | 'board' | 'sprint',
    name: string | undefined
  ): string {
    return name ? `${scopeKindLabels[kind]} · ${name}` : scopeKindLabels[kind];
  }
}
