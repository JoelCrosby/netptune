import { Component, computed, input, model } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { SprintViewModel } from '@core/models/view-models/sprint-view-model';
import {
  AutomationScopeKind,
  scopeKindLabels,
} from '../models/automation-copy';
import { ServiceAccount } from '@core/models/service-account';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';

@Component({
  selector: 'app-automation-settings-editor',
  imports: [
    CheckboxComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
  template: `
    <div class="flex flex-col justify-baseline gap-4">
      <app-form-input
        name="name"
        i18n-label="Label of the name field"
        label="Name"
        [required]="true"
        [(value)]="name" />

      <app-form-select
        name="execution-user"
        i18n-label="
          Label of the field choosing which service account runs the actions
        "
        label="Run as"
        i18n-hint="Explains whose permissions automation actions run with"
        hint="Actions use this service account's workspace permissions and appear as automation activity."
        i18n-placeholder="Placeholder text: Choose a service account"
        placeholder="Choose a service account"
        [required]="true"
        [disabled]="!serviceAccounts().length"
        [(value)]="executionUserId">
        @for (account of serviceAccounts(); track account.userId) {
          <app-form-select-option [value]="account.userId">
            {{ account.name }}
          </app-form-select-option>
        }
      </app-form-select>

      @if (!serviceAccounts().length) {
        <p class="text-muted -mt-3 text-sm">
          <span i18n="Warns that a service account is required">
            Create an enabled service account before saving this automation.
          </span>
        </p>
      }

      <div class="flex flex-col gap-3">
        <app-form-select
          name="scope-kind"
          i18n-label="Label of the scope field"
          label="Scope"
          i18n-hint="Explains what conditions do"
          hint="Limit which tasks this automation can act on."
          [value]="scopeKind()"
          (valueChange)="setScopeKind($event)">
          @for (option of scopeOptions; track option) {
            <app-form-select-option [value]="option">
              {{ scopeLabel(option) }}
            </app-form-select-option>
          }
        </app-form-select>

        @switch (scopeKind()) {
          @case ('project') {
            <app-form-select
              name="scope-project"
              i18n-label="Label of the project field"
              label="Project"
              i18n-placeholder="Placeholder text: Choose a project"
              placeholder="Choose a project"
              [required]="true"
              [(value)]="projectId">
              @for (project of projects(); track project.id) {
                <app-form-select-option [value]="project.id">
                  {{ project.name }}
                </app-form-select-option>
              }
            </app-form-select>
          }
          @case ('board') {
            <app-form-select
              name="scope-board"
              i18n-label="Label of the board field"
              label="Board"
              i18n-placeholder="Placeholder text: Choose a board"
              placeholder="Choose a board"
              [required]="true"
              [(value)]="boardId">
              @for (board of boards(); track board.id) {
                <app-form-select-option [value]="board.id">
                  {{ board.name }}
                </app-form-select-option>
              }
            </app-form-select>
          }
          @case ('sprint') {
            <app-form-select
              name="scope-sprint"
              i18n-label="Label of the sprint field"
              label="Sprint"
              i18n-placeholder="Placeholder text: Choose a sprint"
              placeholder="Choose a sprint"
              [required]="true"
              [(value)]="sprintId">
              @for (sprint of sprints(); track sprint.id) {
                <app-form-select-option [value]="sprint.id">
                  {{ sprint.name }}
                </app-form-select-option>
              }
            </app-form-select>
          }
        }
      </div>

      <div class="border-border bg-foreground/5 rounded-lg border p-4">
        <app-checkbox [(checked)]="isEnabled">
          <span class="flex flex-col">
            <span
              class="text-foreground text-sm font-medium"
              i18n="Marks an automation that is switched on">
              Enabled
            </span>
            <span class="text-muted text-sm">
              <span i18n="Explains the enabled toggle">
                Turn this automation on so it runs automatically.
              </span>
            </span>
          </span>
        </app-checkbox>
      </div>
    </div>
  `,
})
export class AutomationSettingsEditorComponent {
  readonly scopeOptions: AutomationScopeKind[] = [
    'workspace',
    'project',
    'board',
    'sprint',
  ];

  readonly serviceAccounts = input<readonly ServiceAccount[]>([]);
  readonly projects = input<readonly ProjectViewModel[]>([]);
  readonly boards = input<readonly BoardViewModel[]>([]);
  readonly sprints = input<readonly SprintViewModel[]>([]);
  readonly name = model('');
  readonly isEnabled = model(true);
  readonly executionUserId = model<string | null>(null);
  readonly projectId = model<number | null>(null);
  readonly boardId = model<number | null>(null);
  readonly sprintId = model<number | null>(null);

  readonly scopeKind = computed<AutomationScopeKind>(() => {
    if (this.projectId() !== null) return 'project';
    if (this.boardId() !== null) return 'board';
    if (this.sprintId() !== null) return 'sprint';

    return 'workspace';
  });

  scopeLabel(kind: AutomationScopeKind): string {
    return scopeKindLabels[kind];
  }

  setScopeKind(kind: AutomationScopeKind | null) {
    this.projectId.set(kind === 'project' ? this.firstProjectId() : null);
    this.boardId.set(kind === 'board' ? this.firstBoardId() : null);
    this.sprintId.set(kind === 'sprint' ? this.firstSprintId() : null);
  }

  private firstProjectId(): number | null {
    return this.projects()[0]?.id ?? null;
  }

  private firstBoardId(): number | null {
    return this.boards()[0]?.id ?? null;
  }

  private firstSprintId(): number | null {
    return this.sprints()[0]?.id ?? null;
  }
}
