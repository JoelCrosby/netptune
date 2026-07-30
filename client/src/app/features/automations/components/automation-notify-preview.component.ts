import { Component, computed, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import {
  previewNotificationMessage,
  previewNotificationRecipients,
} from '../models/automation-copy';
import { AutomationAction } from '../models/automation.models';

@Component({
  selector: 'app-automation-notify-preview',
  template: `
    <section
      class="border-border rounded-md border"
      i18n-aria-label="Accessible name of the notification preview"
      aria-label="Notification preview">
      <header class="border-border bg-foreground/3 border-b px-3 py-2">
        <h4 class="text-xs font-bold tracking-wider">
          <span i18n="Heading above the notification preview">PREVIEW</span>
        </h4>
      </header>

      <div class="flex flex-col gap-3 p-3 text-sm">
        <div class="flex flex-col gap-1">
          <span
            class="text-foreground/60 text-xs"
            i18n="Label before the notification recipients">
            Notifies
          </span>
          <ul class="flex flex-col gap-1">
            @for (recipient of recipients(); track $index) {
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
          <span
            class="text-foreground/60 text-xs"
            i18n="Label before the notification message">
            Message
          </span>
          <p class="leading-relaxed whitespace-pre-wrap">
            @for (segment of message(); track $index) {
              @if (segment.isUnknown) {
                <span class="text-warn font-medium">{{ segment.text }}</span>
              } @else if (segment.isVariable) {
                <span
                  class="text-primary bg-primary/10 rounded px-1 font-medium">
                  {{ segment.text }}
                </span>
              } @else {
                <span>{{ segment.text }}</span>
              }
            }
          </p>
        </div>

        <p class="text-foreground/60 text-xs">
          <span i18n="Explains highlighted variables in the preview">
            Highlighted values come from variables — the rule fills them in when
            it runs.
          </span>
        </p>
      </div>
    </section>
  `,
})
export class AutomationNotifyPreviewComponent {
  readonly action = input.required<AutomationAction>();
  readonly users = input.required<WorkspaceAppUser[]>();
  readonly ruleName = input('');

  readonly recipients = computed(() => {
    return previewNotificationRecipients(this.action(), this.users());
  });

  readonly message = computed(() => {
    return previewNotificationMessage(this.action().message, this.ruleName());
  });
}
