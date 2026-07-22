import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { StatusesService } from '@core/services/statuses.service';
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
import { AutomationFormPreviewComponent } from '../../components/automation-form-preview.component';
import { AutomationSettingsEditorComponent } from '../../components/automation-settings-editor.component';
import { AutomationTriggerEditorComponent } from '../../components/automation-trigger-editor.component';
import { describeAutomationRule } from '../../models/automation-copy';
import { buildAutomationRuleRequest } from '../../models/automation-rule-request-builder';
import {
  AutomationActionType,
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
          <div class="flex w-full max-w-lg flex-col gap-5">
            <app-stepper>
              <app-step
                title="Settings"
                description="Name your automation and set whether it is active.">
                <app-automation-settings-editor
                  [(name)]="name"
                  [(isEnabled)]="isEnabled" />
              </app-step>

              <app-step
                title="Trigger"
                description="Choose the event that starts this automation.">
                <app-automation-trigger-editor
                  [statuses]="taskStatuses()"
                  [(triggerType)]="triggerType"
                  [(taskFields)]="taskFields"
                  [(status)]="status"
                  [(assigneeChangeMode)]="assigneeChangeMode"
                  [(durationDays)]="durationDays" />
              </app-step>

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
            [savePreview]="savePreview()" />
        </form>
      }
    </app-page-container>
  `,
})
export class AutomationFormViewComponent {
  private service = inject(AutomationsService);
  private statusesService = inject(StatusesService);
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
  readonly defaultCompleteStatusId = computed(
    () => this.statusIdByKey('complete') ?? this.taskStatuses()[0]?.id ?? null
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
  readonly triggerType = signal(AutomationTriggerType.taskChanged);
  readonly taskFields = signal<TaskChangeField[]>([TaskChangeField.status]);

  readonly assigneeChangeMode = signal(AssigneeChangeMode.addedOrRemoved);
  readonly durationDays = signal('3');

  readonly status = signal<number | null>(null);

  constructor() {
    effect(() => {
      const defaultId = this.defaultCompleteStatusId();
      if (defaultId !== null && untracked(this.status) === null) {
        this.status.set(defaultId);
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
        statusId: fields.includes(TaskChangeField.status)
          ? this.status()
          : null,
        assigneeChangeMode: fields.includes(TaskChangeField.assignees)
          ? this.assigneeChangeMode()
          : null,
        durationDays: null,
      };
    }

    return {
      type: this.triggerType(),
      fields: null,
      durationDays: Number(this.durationDays()),
      statusId: null,
      assigneeChangeMode: null,
    };
  });

  readonly savePreview = computed(() =>
    describeAutomationRule(this.triggerPreview(), this.actions())
  );

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
    this.status.set(rule.trigger.statusId ?? this.defaultCompleteStatusId());
    this.assigneeChangeMode.set(
      rule.trigger.assigneeChangeMode ?? AssigneeChangeMode.addedOrRemoved
    );
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
    };
  }

  private statusIdByKey(key: string): number | null {
    return this.taskStatuses().find((status) => status.key === key)?.id ?? null;
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
