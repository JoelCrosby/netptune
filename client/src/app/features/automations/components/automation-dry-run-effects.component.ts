import { Component, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { workspaceRoleLabels } from '@core/enums/workspace-role';
import { joinNaturalList } from '@core/util/strings';
import { actionTypeLabels } from '../models/automation-copy';
import {
  AutomationActionType,
  AutomationDryRunAction,
  AutomationRelationOperation,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-dry-run-effects',
  template: `
    <div class="border-border flex flex-col gap-2 rounded-md border p-3">
      <h5 class="text-xs font-bold tracking-wider">
        <span i18n="Heading above the actions a test run would perform">
          WOULD DO
        </span>
      </h5>

      <ul class="flex flex-col gap-2">
        @for (action of actions(); track action.actionId) {
          <li class="flex flex-col gap-0.5">
            <span class="text-sm font-medium">{{ actionLabel(action) }}</span>

            @if (!action.hasEffect) {
              <span class="text-muted text-xs">
                <span i18n="Shown when a test run would change nothing">
                  No effect for this task — nothing would change.
                </span>
              </span>
            } @else {
              @switch (action.type) {
                @case (automationActionType.notifyTaskAssignees) {
                  <span class="text-muted text-xs">
                    <span
                      i18n="
                        Test-run effect: who would be notified. RECIPIENTS is a
                        list of names
                      ">
                      Notifies
                      {{
                        describeRecipients(action) // i18n(ph="RECIPIENTS")
                      }}
                    </span>
                  </span>
                  @if (action.message) {
                    <span class="text-xs">"{{ action.message }}"</span>
                  }
                }
                @case (automationActionType.addComment) {
                  <span class="text-xs">"{{ action.comment }}"</span>
                }
                @case (automationActionType.flagTask) {
                  <span class="text-muted text-xs">
                    <span
                      i18n="
                        Test-run effect: the flag that would be raised. FLAG is
                        the flag name
                      ">
                      Flags the task as "{{
                        action.flagName  // i18n(ph="FLAG")
                      }}"
                    </span>
                  </span>
                }
                @case (automationActionType.updateTask) {
                  <span class="text-muted text-xs">
                    <span
                      i18n="
                        Test-run effect: which fields would change. FIELDS is a
                        list of field names
                      ">
                      Updates
                      {{
                        describeUpdatedFields(action) // i18n(ph="FIELDS")
                      }}
                    </span>
                  </span>
                }
                @case (automationActionType.createTask) {
                  <span class="text-muted text-xs">
                    <span
                      i18n="
                        Test-run effect: the task that would be created. NAME is
                        the task name
                      ">
                      Creates "{{
                        action.createdTaskName  // i18n(ph="NAME")
                      }}"
                    </span>
                  </span>
                }
                @case (automationActionType.manageTaskRelation) {
                  <span class="text-muted text-xs">
                    {{ describeRelation(action) }}
                  </span>
                }
                @case (automationActionType.deleteTask) {
                  <span class="text-warn text-xs">
                    {{ describeDeletion(action) }}
                  </span>
                }
              }
            }
          </li>
        }
      </ul>
    </div>
  `,
})
export class AutomationDryRunEffectsComponent {
  readonly automationActionType = AutomationActionType;

  readonly actions = input.required<AutomationDryRunAction[]>();
  readonly users = input<WorkspaceAppUser[]>([]);

  actionLabel(action: AutomationDryRunAction): string {
    return actionTypeLabels[action.type];
  }

  describeRecipients(action: AutomationDryRunAction): string {
    const audience = action.recipientUserIds.map((userId) => {
      return this.userName(userId);
    });

    if (action.includeProjectMembers) {
      audience.push("everyone in the task's project");
    }

    for (const role of action.recipientRoles) {
      audience.push(`everyone with the ${workspaceRoleLabels[role]} role`);
    }

    if (!audience.length) return 'nobody';

    return joinNaturalList(audience);
  }

  describeUpdatedFields(action: AutomationDryRunAction): string {
    if (!action.updatedFields.length) return 'nothing';

    return joinNaturalList(action.updatedFields).toLowerCase();
  }

  describeRelation(action: AutomationDryRunAction): string {
    const isRemoval =
      action.relationOperation === AutomationRelationOperation.remove;

    if (isRemoval) {
      return 'Removes the configured task links';
    }

    return 'Links the task to the configured task';
  }

  describeDeletion(action: AutomationDryRunAction): string {
    const delayMinutes = action.delayMinutes ?? 0;

    if (delayMinutes <= 0) return 'Deletes the task';

    return `Deletes the task after ${delayMinutes} minutes`;
  }

  private userName(userId: string): string {
    const user = this.users().find((candidate) => candidate.id === userId);

    return user?.displayName ?? 'Unknown user';
  }
}
