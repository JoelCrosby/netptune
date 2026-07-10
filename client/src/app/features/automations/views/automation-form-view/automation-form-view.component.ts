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
import { SpinnerComponent } from '@static/components/spinner/spinner.component';
import { finalize } from 'rxjs';
import {
  AutomationActionsEditorComponent,
  EditableAutomationAction,
} from '../../components/automation-actions-editor.component';
import { AutomationFormPreviewComponent } from '../../components/automation-form-preview.component';
import { AutomationSettingsEditorComponent } from '../../components/automation-settings-editor.component';
import { AutomationTriggerEditorComponent } from '../../components/automation-trigger-editor.component';
import { describeAutomationRule } from '../../models/automation-copy';
import {
  AutomationActionRequest,
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
    SpinnerComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
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
        <div class="flex h-full flex-col items-center justify-center">
          <app-spinner diameter="32px" />
        </div>
      } @else {
        <form
          class="grid gap-5 lg:grid-cols-[minmax(0,1fr)_360px]"
          (ngSubmit)="onSubmit()">
          <div class="flex w-full max-w-lg flex-col gap-5">
            <app-automation-settings-editor
              [(name)]="name"
              [(isEnabled)]="isEnabled" />

            <app-automation-trigger-editor
              [statuses]="taskStatuses()"
              [(triggerType)]="triggerType"
              [(taskFields)]="taskFields"
              [(status)]="status"
              [(assigneeChangeMode)]="assigneeChangeMode"
              [(durationDays)]="durationDays" />

            <app-automation-actions-editor
              [actions]="actions()"
              [statuses]="taskStatuses()"
              [defaultStatusId]="defaultActiveStatusId()"
              (addAction)="addAction()"
              (removeAction)="removeAction($event)"
              (actionTypeChanged)="
                onActionTypeChanged($event.clientId, $event.type)
              "
              (actionUpdated)="updateAction($event.clientId, $event.patch)" />

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
    // Apply the default status once statuses load. Runs after change
    // detection so the status <app-form-select> options are already
    // initialised — writing the value eagerly during CD races the
    // required `value` input on the options (NG0950).
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
      type: AutomationTriggerType.taskUnassignedFor,
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
    this.validationError.set(null);

    if (!this.name().trim()) {
      this.validationError.set('Automation name is required.');
      return null;
    }

    const actions = this.buildActions();
    if (!actions.length) {
      this.validationError.set('Add at least one action.');
      return null;
    }

    const invalidFlag = actions.some(
      (action) =>
        action.type === AutomationActionType.flagTask &&
        !action.flagName?.trim()
    );

    if (invalidFlag) {
      this.validationError.set('Flag actions need a flag name.');
      return null;
    }

    const invalidUpdate = actions.some(
      (action) =>
        action.type === AutomationActionType.updateTask &&
        action.statusId === null &&
        action.priority === null
    );

    if (invalidUpdate) {
      this.validationError.set(
        'Task update actions need a status or priority.'
      );
      return null;
    }

    const trigger = this.triggerPreview();
    if (
      trigger.type === AutomationTriggerType.taskChanged &&
      !trigger.fields?.length
    ) {
      this.validationError.set('Choose at least one task field to watch.');
      return null;
    }

    if (
      trigger.type === AutomationTriggerType.taskChanged &&
      trigger.fields?.includes(TaskChangeField.status) &&
      trigger.statusId === null
    ) {
      this.validationError.set('Choose a status to watch.');
      return null;
    }

    if (
      trigger.type === AutomationTriggerType.taskUnassignedFor &&
      (!Number.isFinite(trigger.durationDays) ||
        (trigger.durationDays ?? 0) < 1 ||
        (trigger.durationDays ?? 0) > 365)
    ) {
      this.validationError.set('Unassigned duration must be 1 to 365 days.');
      return null;
    }

    return {
      name: this.name().trim(),
      isEnabled: this.isEnabled(),
      trigger,
      actions,
    };
  }

  private buildActions(): AutomationActionRequest[] {
    return this.actions().map((action) => ({
      type: action.type,
      message:
        action.type === AutomationActionType.notifyTaskAssignees
          ? action.message?.trim() || null
          : null,
      flagName:
        action.type === AutomationActionType.flagTask
          ? action.flagName?.trim() || null
          : null,
      flagDescription:
        action.type === AutomationActionType.flagTask
          ? action.flagDescription?.trim() || null
          : null,
      statusId:
        action.type === AutomationActionType.updateTask
          ? (action.statusId ?? null)
          : null,
      priority:
        action.type === AutomationActionType.updateTask
          ? (action.priority ?? null)
          : null,
    }));
  }

  private newNotifyAction(): EditableAutomationAction {
    return {
      clientId: this.nextActionId++,
      type: AutomationActionType.notifyTaskAssignees,
      message: '',
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
