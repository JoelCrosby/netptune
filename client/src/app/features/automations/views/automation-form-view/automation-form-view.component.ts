import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TaskStatus } from '@core/enums/project-task-status';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
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
          <div class="flex flex-col gap-5">
            <app-automation-settings-editor
              [(name)]="name"
              [(isEnabled)]="isEnabled" />

            <app-automation-trigger-editor
              [(triggerType)]="triggerType"
              [(taskFields)]="taskFields"
              [(status)]="status"
              [(assigneeChangeMode)]="assigneeChangeMode"
              [(durationDays)]="durationDays" />

            <app-automation-actions-editor
              [actions]="actions()"
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
  private snackbar = inject(SnackbarService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly validationError = signal<string | null>(null);

  private nextActionId = 1;

  readonly actions = signal<EditableAutomationAction[]>([
    this.newNotifyAction(),
  ]);

  name = '';
  isEnabled = true;
  triggerType = AutomationTriggerType.taskChanged;
  taskFields: TaskChangeField[] = [TaskChangeField.status];
  status = TaskStatus.complete;
  assigneeChangeMode = AssigneeChangeMode.addedOrRemoved;
  durationDays = '3';

  constructor() {
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
    });
  }

  updateAction(clientId: number, patch: Partial<EditableAutomationAction>) {
    this.actions.update((actions) =>
      actions.map((action) =>
        action.clientId === clientId ? { ...action, ...patch } : action
      )
    );
  }

  triggerPreview(): AutomationTrigger {
    return this.buildTrigger();
  }

  savePreview(): string {
    return describeAutomationRule(this.buildTrigger(), this.actions());
  }

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
    this.name = rule.name;
    this.isEnabled = rule.isEnabled;
    this.triggerType =
      rule.trigger.type === AutomationTriggerType.taskStatusChanged
        ? AutomationTriggerType.taskChanged
        : rule.trigger.type;
    this.taskFields = rule.trigger.fields?.length
      ? rule.trigger.fields
      : [TaskChangeField.status];
    this.status = rule.trigger.status ?? TaskStatus.complete;
    this.assigneeChangeMode =
      rule.trigger.assigneeChangeMode ?? AssigneeChangeMode.addedOrRemoved;
    this.durationDays = String(rule.trigger.durationDays ?? 3);
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

    if (!this.name.trim()) {
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

    const trigger = this.buildTrigger();
    if (
      trigger.type === AutomationTriggerType.taskChanged &&
      !trigger.fields?.length
    ) {
      this.validationError.set('Choose at least one task field to watch.');
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
      name: this.name.trim(),
      isEnabled: this.isEnabled,
      trigger,
      actions,
    };
  }

  private buildTrigger(): AutomationTrigger {
    if (this.triggerType === AutomationTriggerType.taskChanged) {
      const fields = [...new Set(this.taskFields)];

      return {
        type: AutomationTriggerType.taskChanged,
        fields,
        status: fields.includes(TaskChangeField.status) ? this.status : null,
        assigneeChangeMode: fields.includes(TaskChangeField.assignees)
          ? this.assigneeChangeMode
          : null,
        durationDays: null,
      };
    }

    return {
      type: AutomationTriggerType.taskUnassignedFor,
      fields: null,
      durationDays: Number(this.durationDays),
      status: null,
      assigneeChangeMode: null,
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
    }));
  }

  private newNotifyAction(): EditableAutomationAction {
    return {
      clientId: this.nextActionId++,
      type: AutomationActionType.notifyTaskAssignees,
      message: '',
      flagName: null,
      flagDescription: null,
    };
  }

  private ruleId(): number | null {
    const value = Number(this.route.snapshot.paramMap.get('id'));
    return Number.isFinite(value) && value > 0 ? value : null;
  }
}
