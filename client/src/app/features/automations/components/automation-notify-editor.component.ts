import { Component, computed, input, output } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import {
  messageVariables,
  notificationRecipientLabels,
} from '../models/automation-copy';
import {
  AutomationAction,
  AutomationNotificationRecipient,
} from '../models/automation.models';

@Component({
  selector: 'app-automation-notify-editor',
  imports: [
    FormSelectTagsComponent,
    FormSelectTagsOptionComponent,
    FormTextAreaComponent,
  ],
  template: `
    <div class="flex flex-col gap-4">
      <app-form-select-tags
        label="Recipients"
        placeholder="Choose who to notify"
        [value]="selectedRecipients()"
        (changed)="setRecipients($event)">
        @for (recipient of recipientOptions; track recipient) {
          <app-form-select-tags-option [value]="recipient">
            {{ recipientLabel(recipient) }}
          </app-form-select-tags-option>
        }
      </app-form-select-tags>

      @if (targetsSpecificUsers()) {
        <app-form-select-tags
          label="Users"
          placeholder="Choose the users to notify"
          [value]="action().recipientUserIds ?? []"
          (changed)="patch.emit({ recipientUserIds: $event })">
          @for (user of users(); track user.id) {
            <app-form-select-tags-option [value]="user.id">
              {{ user.displayName }}
            </app-form-select-tags-option>
          }
        </app-form-select-tags>
      }

      @if (targetsRoles()) {
        <app-form-select-tags
          label="Workspace roles"
          placeholder="Choose the roles to notify"
          [value]="action().recipientRoles ?? []"
          (changed)="patch.emit({ recipientRoles: $event })">
          @for (role of roleOptions; track role) {
            <app-form-select-tags-option [value]="role">
              {{ roleLabel(role) }}
            </app-form-select-tags-option>
          }
        </app-form-select-tags>
      }

      <app-form-textarea
        label="Message"
        rows="3"
        [noMargin]="true"
        [hint]="variableHint"
        [value]="action().message ?? ''"
        (valueChange)="patch.emit({ message: $event })" />
    </div>
  `,
})
export class AutomationNotifyEditorComponent {
  recipientOptions = [
    AutomationNotificationRecipient.assignees,
    AutomationNotificationRecipient.taskOwner,
    AutomationNotificationRecipient.triggeringUser,
    AutomationNotificationRecipient.specificUsers,
    AutomationNotificationRecipient.projectMembers,
    AutomationNotificationRecipient.workspaceRoles,
  ];

  roleOptions = [
    WorkspaceRole.viewer,
    WorkspaceRole.member,
    WorkspaceRole.admin,
    WorkspaceRole.owner,
  ];

  variableHint = `Variables: ${messageVariables
    .map((variable) => `{{${variable}}}`)
    .join(' ')}`;

  action = input.required<AutomationAction>();
  users = input.required<WorkspaceAppUser[]>();
  patch = output<Partial<AutomationAction>>();

  selectedRecipients = computed(() => {
    return (
      this.action().recipients ?? [AutomationNotificationRecipient.assignees]
    );
  });

  targetsSpecificUsers = computed(() => {
    return this.selectedRecipients().includes(
      AutomationNotificationRecipient.specificUsers
    );
  });

  targetsRoles = computed(() => {
    return this.selectedRecipients().includes(
      AutomationNotificationRecipient.workspaceRoles
    );
  });

  recipientLabel(recipient: AutomationNotificationRecipient): string {
    return notificationRecipientLabels[recipient];
  }

  roleLabel(role: WorkspaceRole): string {
    return workspaceRoleLabels[role];
  }

  setRecipients(recipients: AutomationNotificationRecipient[]) {
    const targetsUsers = recipients.includes(
      AutomationNotificationRecipient.specificUsers
    );
    const targetsRoles = recipients.includes(
      AutomationNotificationRecipient.workspaceRoles
    );

    this.patch.emit({
      recipients,
      recipientUserIds: targetsUsers
        ? (this.action().recipientUserIds ?? [])
        : [],
      recipientRoles: targetsRoles ? (this.action().recipientRoles ?? []) : [],
    });
  }
}
