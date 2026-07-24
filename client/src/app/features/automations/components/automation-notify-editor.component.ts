import { Component, computed, input, output } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { WorkspaceRole, workspaceRoleLabels } from '@core/enums/workspace-role';
import { FormSelectTagsOptionComponent } from '@static/components/form-select-tags/form-select-tags-option.component';
import { FormSelectTagsComponent } from '@static/components/form-select-tags/form-select-tags.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import {
  messageVariables,
  notificationRecipientLabels,
  previewNotificationMessage,
  previewNotificationRecipients,
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

      <section
        class="border-border rounded-md border"
        aria-label="Notification preview">
        <header class="border-border bg-foreground/3 border-b px-3 py-2">
          <h4 class="text-xs font-bold tracking-wider">PREVIEW</h4>
        </header>

        <div class="flex flex-col gap-3 p-3 text-sm">
          <div class="flex flex-col gap-1">
            <span class="text-foreground/60 text-xs">Notifies</span>
            <ul class="flex flex-col gap-1">
              @for (recipient of recipientPreview(); track $index) {
                <li
                  class="flex items-start gap-2"
                  [class.text-warn]="recipient.isIncomplete">
                  <span aria-hidden="true">&bull;</span>
                  <span>{{ recipient.text }}</span>
                </li>
              }
            </ul>
          </div>

          <div class="flex flex-col gap-1">
            <span class="text-foreground/60 text-xs">Message</span>
            <p class="leading-relaxed whitespace-pre-wrap">
              {{ messagePreview() }}
            </p>
          </div>

          <p class="text-foreground/60 text-xs">
            Task values are examples — the rule fills them in when it runs.
          </p>
        </div>
      </section>
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
  ruleName = input('');
  patch = output<Partial<AutomationAction>>();

  recipientPreview = computed(() => {
    return previewNotificationRecipients(this.action(), this.users());
  });

  messagePreview = computed(() => {
    return previewNotificationMessage(this.action().message, this.ruleName());
  });

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
