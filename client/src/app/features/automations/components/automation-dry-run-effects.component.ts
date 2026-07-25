import { Component, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { workspaceRoleLabels } from '@core/enums/workspace-role';
import { joinNaturalList } from '@core/util/strings';
import { actionTypeLabels } from '../models/automation-copy';
import {
  AutomationActionType,
  AutomationDryRunAction,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-dry-run-effects',
  template: `
    <div class="border-border flex flex-col gap-2 rounded-md border p-3">
      <h5 class="text-xs font-bold tracking-wider">WOULD DO</h5>

      <ul class="flex flex-col gap-2">
        @for (action of actions(); track action.actionId) {
          <li class="flex flex-col gap-0.5">
            <span class="text-sm font-medium">{{ actionLabel(action) }}</span>

            @if (!action.hasEffect) {
              <span class="text-muted text-xs">
                No effect for this task — nothing would change.
              </span>
            } @else {
              @switch (action.type) {
                @case (automationActionType.notifyTaskAssignees) {
                  <span class="text-muted text-xs">
                    Notifies {{ describeRecipients(action) }}
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
                    Flags the task as "{{ action.flagName }}"
                  </span>
                }
                @case (automationActionType.updateTask) {
                  <span class="text-muted text-xs">
                    Updates {{ describeUpdatedFields(action) }}
                  </span>
                }
                @case (automationActionType.createTask) {
                  <span class="text-muted text-xs">
                    Creates "{{ action.createdTaskName }}"
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
