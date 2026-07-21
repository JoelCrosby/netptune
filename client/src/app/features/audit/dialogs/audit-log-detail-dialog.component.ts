import { JsonPipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import { DIALOG_DATA } from '@angular/cdk/dialog';
import { Component, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { AuditLogDetailViewModel } from '@core/models/view-models/audit-log-view-model';
import { activityTypeToString } from '@core/transforms/activity-type';
import { entityTypeToString } from '@core/transforms/entity-type';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import {
  PropertyListComponent,
  PropertyListItem,
} from '@static/components/property-list/property-list.component';

export interface AuditLogDetailDialogData {
  id: number;
}

@Component({
  selector: 'app-audit-log-detail-dialog',
  imports: [
    DialogContentComponent,
    DialogTitleComponent,
    JsonPipe,
    PropertyListComponent,
  ],
  template: `
    <app-dialog-title showCloseButton>Audit log details</app-dialog-title>

    <app-dialog-content>
      @if (detail.isLoading()) {
        <p class="text-muted py-12 text-center text-sm">Loading details…</p>
      } @else if (detail.error()) {
        <p class="py-12 text-center text-sm text-red-500">
          Failed to load audit log details.
        </p>
      } @else if (detail.value()?.payload; as log) {
        <div class="max-h-[70vh] space-y-6 overflow-y-auto pr-2">
          <section>
            <h2 class="text-muted mb-2 text-xs font-medium uppercase">
              Summary
            </h2>
            <p class="text-sm">{{ log.summary }}</p>
          </section>

          <app-property-list heading="Event" [items]="eventProperties(log)" />

          <app-property-list
            heading="Subject"
            [items]="subjectProperties(log)" />

          <app-property-list
            heading="Context"
            [items]="contextProperties(log)" />

          <app-property-list
            heading="Request"
            [items]="requestProperties(log)" />

          @if (log.references.length > 0) {
            <section>
              <h2 class="text-muted mb-2 text-xs font-medium uppercase">
                References
              </h2>
              <div class="border-border overflow-hidden rounded border">
                @for (
                  reference of log.references;
                  track reference.role +
                    reference.entityType +
                    reference.entityId
                ) {
                  <div
                    class="border-border grid grid-cols-[7rem_1fr] gap-3 border-b px-3 py-2 text-xs last:border-b-0">
                    <span class="text-muted">{{ reference.role }}</span>
                    <span class="font-mono break-all">
                      {{ reference.entityType }}:{{ reference.entityId }}
                    </span>
                  </div>
                }
              </div>
            </section>
          }

          <section>
            <h2 class="text-muted mb-2 text-xs font-medium uppercase">
              Payload
            </h2>
            <pre
              class="bg-foreground/5 max-w-full overflow-x-auto rounded p-4 text-xs leading-5"><code>{{ log.meta | json }}</code></pre>
          </section>
        </div>
      }
    </app-dialog-content>
  `,
})
export class AuditLogDetailDialogComponent {
  static readonly width = '720px';

  private readonly data = inject<AuditLogDetailDialogData>(DIALOG_DATA);

  protected readonly detail = httpResource<
    ClientResponse<AuditLogDetailViewModel>
  >(() => `api/audit/${this.data.id}`);

  protected eventProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      {
        label: 'Event key',
        value: log.eventKey,
        monospace: true,
        breakAll: true,
      },
      { label: 'Log ID', value: log.id },
      {
        label: 'Event ID',
        value: log.eventId,
        monospace: true,
        breakAll: true,
      },
      { label: 'Schema version', value: log.schemaVersion },
      { label: 'Occurred', value: log.occurredAt, format: 'date' },
      { label: 'Recorded', value: log.recordedAt, format: 'date' },
      { label: 'Actor', value: log.userDisplayName },
      { label: 'Actor ID', value: log.userId, monospace: true, breakAll: true },
      { label: 'Action', value: activityTypeToString(log.type) },
      { label: 'Retention', value: log.retentionClass },
    ];
  }

  protected subjectProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      { label: 'Type', value: log.subjectType },
      { label: 'Entity type', value: entityTypeToString(log.entityType) },
      { label: 'ID', value: log.subjectId, monospace: true, breakAll: true },
      { label: 'Sequence', value: log.subjectSequence },
      {
        label: 'Correlation ID',
        value: log.correlationId,
        monospace: true,
        breakAll: true,
      },
      {
        label: 'Causation event ID',
        value: log.causationEventId,
        monospace: true,
        breakAll: true,
      },
    ];
  }

  protected contextProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      { label: 'Workspace', value: log.workspaceSlug },
      { label: 'Project', value: log.projectSlug },
      { label: 'Board', value: log.boardSlug },
    ];
  }

  protected requestProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      { label: 'IP address', value: log.ipAddress },
      { label: 'User agent', value: log.userAgent, breakAll: true },
    ];
  }
}
