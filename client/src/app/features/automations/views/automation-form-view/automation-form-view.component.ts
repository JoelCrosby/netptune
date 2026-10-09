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
import { mutation } from '@core/util/mutation';
import {
  LucideChevronLeft,
  LucideChevronRight,
  LucideCircleAlert,
  LucideListFilter,
  LucidePlus,
  LucideSettings2,
  LucideZap,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { FormControlShapeDirective } from '@static/components/form-control/form-control.directives';
import { HeadingInputDirective } from '@static/components/form-input/heading-input.directive';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageLoadingComponent } from '@static/components/page-loading/page-loading.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { SwitchComponent } from '@static/components/switch/switch.component';
import {
  AutomationActionEditorComponent,
  EditableAutomationAction,
  automationActionLimit,
} from '../../components/automation-action-editor.component';
import { AutomationConditionsEditorComponent } from '../../components/automation-conditions-editor.component';
import { AutomationDescriptionComponent } from '../../components/automation-description.component';
import { AutomationFlowNodeComponent } from '../../components/automation-flow-node.component';
import { AutomationSettingsEditorComponent } from '../../components/automation-settings-editor.component';
import { AutomationTriggerEditorComponent } from '../../components/automation-trigger-editor.component';
import {
  AutomationCopySegment,
  actionTypeDescriptions,
  actionTypeLabels,
  countAutomationConditions,
  describeAutomationActionSegments,
  triggerTypeLabels,
} from '../../models/automation-copy';
import {
  automationActionKeyword,
  describeAutomationScope,
  describeConditionsStepSummary,
  describeConditionsStepTitle,
  describeServiceAccountName,
  describeTriggerStepSummary,
  resolveAutomationScope,
} from '../../models/automation-flow-copy';
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

type AutomationFlowStepKey = 'settings' | 'trigger' | 'conditions' | number;

@Component({
  selector: 'app-automation-form-view',
  imports: [
    AutomationActionEditorComponent,
    AutomationConditionsEditorComponent,
    AutomationDescriptionComponent,
    AutomationFlowNodeComponent,
    AutomationSettingsEditorComponent,
    AutomationTriggerEditorComponent,
    CalloutComponent,
    FlatButtonComponent,
    FormControlShapeDirective,
    HeadingInputDirective,
    LucideChevronLeft,
    LucideChevronRight,
    LucidePlus,
    PageContainerComponent,
    PageLoadingComponent,
    RouterLink,
    StrokedButtonComponent,
    SwitchComponent,
  ],
  template: `
    <app-page-container layout="list">
      @if (loading()) {
        <app-page-loading />
      } @else {
        <form
          appFormShape="rounded"
          class="flex min-h-0 flex-1 flex-col"
          (ngSubmit)="onSubmit()">
          <header
            class="border-border flex shrink-0 items-center gap-4 border-b px-7 pt-5 pb-4.5 max-md:flex-wrap max-md:px-3 max-md:pt-3">
            <div class="min-w-0 flex-1 max-md:basis-full">
              <p class="text-foreground/50 mb-0.5 text-[13px] font-medium">
                @if (isEdit()) {
                  <span
                    i18n="Eyebrow above the name of an automation being edited">
                    Edit automation
                  </span>
                } @else {
                  <span
                    i18n="
                      Eyebrow above the name of an automation being created
                    ">
                    Create automation
                  </span>
                }
              </p>

              <input
                appHeadingInput
                class="text-2xl/[30px] font-bold tracking-[-0.3px]"
                type="text"
                name="name"
                autocomplete="off"
                required
                i18n-placeholder="
                  Placeholder in the empty automation name field
                "
                placeholder="Untitled automation"
                i18n-aria-label="Label of the automation name field"
                aria-label="Automation name"
                [value]="name()"
                (input)="onNameInput($event)" />
            </div>

            <div
              class="text-foreground/75 flex shrink-0 items-center gap-2.5 text-sm font-semibold">
              <app-switch
                i18n-ariaLabel="
                  Accessible label of the automation enabled switch
                "
                ariaLabel="Enabled"
                [(checked)]="isEnabled" />
              <span class="min-w-15">
                @if (isEnabled()) {
                  <span i18n="Marks an automation that is switched on">
                    Enabled
                  </span>
                } @else {
                  <span i18n="Marks an automation that is switched off">
                    Paused
                  </span>
                }
              </span>
            </div>

            <span
              class="bg-border h-6 w-px shrink-0 max-md:hidden"
              aria-hidden="true"></span>

            <div class="flex shrink-0 items-center gap-2 max-md:ml-auto">
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
                [disabled]="saving.pending()"
                (click)="onSubmit()">
                {{ isEdit() ? 'Save Automation' : 'Create Automation' }}
              </button>
            </div>
          </header>

          <div
            class="min-h-0 flex-1 overflow-y-auto lg:grid lg:grid-cols-[380px_minmax(0,1fr)] lg:overflow-hidden">
            <nav
              class="border-border bg-foreground/2 flex flex-col px-7 pt-6 pb-8 max-lg:border-b max-md:px-3 lg:overflow-y-auto lg:border-r lg:pr-6"
              i18n-aria-label="Accessible label of the automation steps list"
              aria-label="Automation steps">
              <app-automation-flow-node
                i18n-keyword="Heading of the setup part of the rule"
                keyword="SETUP"
                [icon]="setupIcon"
                [title]="runAsName()"
                [selected]="selected() === 'settings'"
                (selectNode)="select('settings')">
                {{ scopeSummary() }}
              </app-automation-flow-node>

              <div class="h-5" aria-hidden="true"></div>

              <app-automation-flow-node
                i18n-keyword="Heading of the trigger part of the rule"
                keyword="WHEN"
                [icon]="triggerIcon"
                [title]="triggerTitle()"
                [selected]="selected() === 'trigger'"
                (selectNode)="select('trigger')">
                <app-automation-description
                  [segments]="triggerSummary()"
                  [statuses]="taskStatuses()" />
              </app-automation-flow-node>

              <div class="bg-border ml-6 h-3 w-px" aria-hidden="true"></div>

              <app-automation-flow-node
                i18n-keyword="Heading of the conditions part of the rule"
                keyword="IF"
                [icon]="conditionsIcon"
                [title]="conditionsTitle()"
                [selected]="selected() === 'conditions'"
                (selectNode)="select('conditions')">
                @if (!conditionCount()) {
                  <span
                    flowNodeAside
                    class="text-foreground/45 text-xs"
                    i18n="Marks the conditions section as not required">
                    Optional
                  </span>
                }
                <app-automation-description
                  [segments]="conditionsSummary()"
                  [statuses]="taskStatuses()" />
              </app-automation-flow-node>

              @for (
                action of actions();
                track action.clientId;
                let index = $index
              ) {
                <div class="bg-border ml-6 h-3 w-px" aria-hidden="true"></div>

                <app-automation-flow-node
                  [keyword]="actionKeyword(index)"
                  [step]="index + 1"
                  [title]="actionTypeLabel(action)"
                  [selected]="selected() === action.clientId"
                  (selectNode)="select(action.clientId)">
                  <app-automation-description
                    [segments]="actionSummary(action)"
                    [statuses]="taskStatuses()" />
                </app-automation-flow-node>
              }

              <div class="bg-border ml-6 h-3 w-px" aria-hidden="true"></div>

              <button
                type="button"
                class="border-border text-foreground/60 hover:border-primary hover:text-primary focus-visible:ring-primary flex h-11.5 w-full cursor-pointer items-center gap-2 rounded-[10px] border border-dashed px-4 text-sm font-semibold transition-colors focus-visible:ring-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                [disabled]="atActionLimit()"
                (click)="addAction()">
                <svg lucidePlus class="h-3.5 w-3.5"></svg>
                <span i18n="Button that adds another automation action">
                  Add action
                </span>
                <span class="text-foreground/45 ml-auto text-xs font-medium">
                  <span
                    i18n="
                      How many automation actions are in use. USED is that
                      count, LIMIT the maximum
                    ">
                    {{
                      actions().length // i18n(ph="USED")
                    }}
                    of
                    {{
                      actionLimit // i18n(ph="LIMIT")
                    }}
                  </span>
                </span>
              </button>
            </nav>

            <section class="bg-card flex min-h-0 flex-col">
              <div
                class="min-h-0 flex-1 px-10 pt-7 pb-9 max-md:px-3 lg:overflow-y-auto">
                <div class="flex max-w-150 flex-col">
                  @if (validationError(); as error) {
                    <app-callout
                      class="mb-5"
                      color="warn"
                      role="alert"
                      [icon]="errorIcon">
                      {{ error }}
                    </app-callout>
                  }

                  <p class="text-foreground/45 mb-2 text-xs font-semibold">
                    <span
                      i18n="
                        Position of the open step in the automation. STEP is its
                        number, TOTAL the number of steps
                      ">
                      Step
                      {{
                        selectedIndex() + 1 // i18n(ph="STEP")
                      }}
                      of
                      {{
                        flowOrder().length // i18n(ph="TOTAL")
                      }}
                    </span>
                  </p>

                  <div class="mb-5.5">
                    <h2 class="mb-1 text-lg font-bold">{{ panelTitle() }}</h2>
                    <p
                      class="text-foreground/55 text-[13px] leading-normal text-pretty">
                      {{ panelDescription() }}
                    </p>
                  </div>

                  @switch (selected()) {
                    @case ('settings') {
                      <app-automation-settings-editor
                        [serviceAccounts]="enabledServiceAccounts()"
                        [projects]="projectsResource.value()"
                        [boards]="workspaceBoards()"
                        [sprints]="workspaceSprintsResource.value()"
                        [(executionUserId)]="executionUserId"
                        [(projectId)]="projectId"
                        [(boardId)]="boardId"
                        [(sprintId)]="sprintId" />
                    }
                    @case ('trigger') {
                      <app-automation-trigger-editor
                        [(triggerType)]="triggerType"
                        [(taskFields)]="taskFields"
                        [(durationDays)]="durationDays" />
                    }
                    @case ('conditions') {
                      <app-automation-conditions-editor
                        [statuses]="taskStatuses()"
                        [supportsChangeOperators]="
                          triggerType() === automationTriggerType.taskChanged
                        "
                        [(conditionGroup)]="conditionGroup" />
                    }
                    @default {
                      @if (selectedAction(); as action) {
                        <app-automation-action-editor
                          [action]="action"
                          [canRemove]="actions().length > 1"
                          [statuses]="taskStatuses()"
                          [users]="workspaceUsers()"
                          [ruleName]="name()"
                          [tags]="workspaceTagsResource.value()"
                          [sprints]="workspaceSprintsResource.value()"
                          [boardGroups]="workspaceBoardGroupsResource.value()"
                          [relationTypes]="relationTypesResource.value()"
                          [defaultStatusId]="defaultActiveStatusId()"
                          (typeChanged)="
                            onActionTypeChanged(action.clientId, $event)
                          "
                          (patch)="updateAction(action.clientId, $event)"
                          (remove)="removeAction(action.clientId)" />
                      }
                    }
                  }
                </div>
              </div>

              <div
                class="border-border flex shrink-0 items-center gap-2.5 border-t px-10 py-3 max-md:px-3">
                @if (previousStep(); as previous) {
                  <button
                    app-stroked-button
                    class="gap-1.5"
                    type="button"
                    (click)="select(previous)">
                    <svg lucideChevronLeft class="h-3.5 w-3.5"></svg>
                    <span>{{ stepName(previous) }}</span>
                  </button>
                }

                <span class="flex-1"></span>

                @if (nextStep(); as next) {
                  <button
                    app-stroked-button
                    class="gap-1.5"
                    type="button"
                    (click)="select(next)">
                    <span>{{ stepName(next) }}</span>
                    <svg lucideChevronRight class="h-3.5 w-3.5"></svg>
                  </button>
                }
              </div>
            </section>
          </div>
        </form>
      }
    </app-page-container>
  `,
})
export class AutomationFormViewComponent {
  readonly automationTriggerType = AutomationTriggerType;
  readonly actionLimit = automationActionLimit;
  readonly setupIcon = LucideSettings2;
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

  readonly saving = mutation();
  readonly validationError = signal<string | null>(null);
  readonly selected = signal<AutomationFlowStepKey>('trigger');

  readonly taskStatusesResource = statusResource();
  readonly serviceAccountsResource = serviceAccountResource();
  readonly workspaceUsersResource = userResource();
  readonly workspaceTagsResource = tagResource();
  readonly workspaceSprintsResource = sprintResource([]);
  readonly workspaceBoardGroupsResource = boardGroupOptionsResource();
  readonly relationTypesResource = relationTypeResource();
  readonly projectsResource = projectResource();
  readonly workspaceBoardsResource = workspaceBoardsResource();

  readonly workspaceBoards = this.workspaceBoardsResource.value;
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

  readonly flowOrder = computed<AutomationFlowStepKey[]>(() => {
    return [
      'settings',
      'trigger',
      'conditions',
      ...this.actions().map((action) => action.clientId),
    ];
  });

  readonly selectedIndex = computed(() => {
    return Math.max(0, this.flowOrder().indexOf(this.selected()));
  });

  readonly previousStep = computed(() => {
    return this.flowOrder()[this.selectedIndex() - 1] ?? null;
  });

  readonly nextStep = computed(() => {
    return this.flowOrder()[this.selectedIndex() + 1] ?? null;
  });

  readonly selectedAction = computed(() => {
    const selected = this.selected();

    return (
      this.actions().find((action) => action.clientId === selected) ?? null
    );
  });

  readonly runAsName = computed(() => {
    return describeServiceAccountName(
      this.serviceAccounts(),
      this.executionUserId()
    );
  });

  readonly scopeSummary = computed(() => {
    const scope = resolveAutomationScope(
      {
        projectId: this.projectId(),
        boardId: this.boardId(),
        sprintId: this.sprintId(),
      },
      {
        projects: this.projectsResource.value(),
        boards: this.workspaceBoards(),
        sprints: this.workspaceSprintsResource.value(),
      }
    );

    return describeAutomationScope(scope);
  });

  readonly triggerTitle = computed(() => {
    return triggerTypeLabels[this.triggerType()];
  });

  readonly triggerSummary = computed(() => {
    return describeTriggerStepSummary(this.triggerPreview());
  });

  readonly conditionCount = computed(() => {
    return countAutomationConditions(this.conditionGroup());
  });

  readonly conditionsTitle = computed(() => {
    return describeConditionsStepTitle(this.triggerPreview());
  });

  readonly conditionsSummary = computed(() => {
    return describeConditionsStepSummary(
      this.triggerPreview(),
      this.taskStatuses()
    );
  });

  readonly panelTitle = computed(() => {
    const action = this.selectedAction();

    if (action) return actionTypeLabels[action.type];

    switch (this.selected()) {
      case 'settings':
        return $localize`:Heading of the automation settings step:Settings`;
      case 'conditions':
        return $localize`:Heading of the automation conditions step:Conditions`;
      default:
        return $localize`:Heading of the automation trigger step:Trigger`;
    }
  });

  readonly panelDescription = computed(() => {
    const action = this.selectedAction();

    if (action) return actionTypeDescriptions[action.type];

    switch (this.selected()) {
      case 'settings':
        return $localize`:Description of the automation settings step:Who the automation acts as, and where it listens.`;
      case 'conditions':
        return $localize`:Description of the automation conditions step:Optional filters. Tasks that don't match stop here.`;
      default:
        return $localize`:Description of the automation trigger step:The event that starts this automation.`;
    }
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

  select(step: AutomationFlowStepKey) {
    this.selected.set(step);
  }

  stepName(step: AutomationFlowStepKey): string {
    switch (step) {
      case 'settings':
        return $localize`:Name of the automation setup step:Setup`;
      case 'trigger':
        return $localize`:Name of the automation trigger step:Trigger`;
      case 'conditions':
        return $localize`:Name of the automation conditions step:Conditions`;
    }

    const position =
      this.actions().findIndex((action) => action.clientId === step) + 1;

    return $localize`:Name of an automation action step. NUMBER is its position:Action ${position}:NUMBER:`;
  }

  actionKeyword(index: number): string {
    return automationActionKeyword(index);
  }

  actionTypeLabel(action: EditableAutomationAction): string {
    return actionTypeLabels[action.type];
  }

  actionSummary(action: EditableAutomationAction): AutomationCopySegment[] {
    return describeAutomationActionSegments(action, this.taskStatuses());
  }

  atActionLimit(): boolean {
    return this.actions().length >= automationActionLimit;
  }

  onNameInput(event: Event) {
    this.name.set((event.target as HTMLInputElement).value);
  }

  addAction() {
    if (this.atActionLimit()) {
      return;
    }

    const action = this.newNotifyAction();

    this.actions.update((actions) => [...actions, action]);
    this.selected.set(action.clientId);
  }

  removeAction(clientId: number) {
    if (this.actions().length === 1) {
      return;
    }

    const previous = this.previousStep();

    this.actions.update((actions) => {
      return actions.filter((action) => action.clientId !== clientId);
    });

    if (this.selected() === clientId && previous !== null) {
      this.selected.set(previous);
    }
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

    this.saving.run(save.pipe(takeUntilDestroyed(this.destroyRef)), {
      onSuccess: (rule) => {
        this.snackbar.open(id ? 'Automation updated' : 'Automation created');
        void this.router.navigate(id ? ['../'] : ['../', rule.id], {
          relativeTo: this.route,
        });
      },
      onError: () => {
        this.snackbar.error(
          $localize`:Error after failing to save an automation:Automation could not be saved`
        );
      },
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
}
