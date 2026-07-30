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
    <app-dialog-title
      showCloseButton
      i18n="Title of the audit entry detail dialog">
      Audit log details
    </app-dialog-title>

    <app-dialog-content>
      @if (detail.isLoading()) {
        <p class="text-muted py-12 text-center text-sm">
          <span i18n="Shown while audit entry details load">
            Loading details…
          </span>
        </p>
      } @else if (detail.error()) {
        <p class="py-12 text-center text-sm text-red-500">
          <span i18n="Shown when audit entry details fail to load">
            Failed to load audit log details.
          </span>
        </p>
      } @else if (detail.value()?.payload; as log) {
        <div class="max-h-[70vh] space-y-6 overflow-y-auto pr-2">
          <section>
            <h2 class="text-muted mb-2 text-xs font-medium uppercase">
              <span i18n="Heading above the audit entry summary">Summary</span>
            </h2>
            <p class="text-sm">{{ log.summary }}</p>
          </section>

          <app-property-list
            i18n-heading="Heading above the audit event properties"
            heading="Event"
            [items]="eventProperties(log)" />

          <app-property-list
            i18n-heading="Heading above the audited subject properties"
            heading="Subject"
            [items]="subjectProperties(log)" />

          <app-property-list
            i18n-heading="Heading above the audit context properties"
            heading="Context"
            [items]="contextProperties(log)" />

          <app-property-list
            i18n-heading="Heading above the audit request properties"
            heading="Request"
            [items]="requestProperties(log)" />

          @if (log.references.length > 0) {
            <section>
              <h2 class="text-muted mb-2 text-xs font-medium uppercase">
                <span i18n="Heading above related records in an audit entry">
                  References
                </span>
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
              <span i18n="Heading above the raw audit event payload">
                Payload
              </span>
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
        label: $localize`:Label shown in the interface:Event key`,
        value: log.eventKey,
        monospace: true,
        breakAll: true,
      },
      { label: $localize`:Label shown in the interface:Log ID`, value: log.id },
      {
        label: $localize`:Label shown in the interface:Event ID`,
        value: log.eventId,
        monospace: true,
        breakAll: true,
      },
      {
        label: $localize`:Label shown in the interface:Schema version`,
        value: log.schemaVersion,
      },
      {
        label: $localize`:Label shown in the interface:Occurred`,
        value: log.occurredAt,
        format: 'date',
      },
      {
        label: $localize`:Label shown in the interface:Recorded`,
        value: log.recordedAt,
        format: 'date',
      },
      {
        label: $localize`:Label shown in the interface:Actor`,
        value: log.userDisplayName,
      },
      {
        label: $localize`:Label shown in the interface:Actor ID`,
        value: log.userId,
        monospace: true,
        breakAll: true,
      },
      {
        label: $localize`:Label shown in the interface:Action`,
        value: activityTypeToString(log.type),
      },
      {
        label: $localize`:Label shown in the interface:Retention`,
        value: log.retentionClass,
      },
    ];
  }

  protected subjectProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      {
        label: $localize`:Label shown in the interface:Type`,
        value: log.subjectType,
      },
      {
        label: $localize`:Label shown in the interface:Entity type`,
        value: entityTypeToString(log.entityType),
      },
      {
        label: $localize`:Label shown in the interface:ID`,
        value: log.subjectId,
        monospace: true,
        breakAll: true,
      },
      {
        label: $localize`:Label shown in the interface:Sequence`,
        value: log.subjectSequence,
      },
      {
        label: $localize`:Label shown in the interface:Correlation ID`,
        value: log.correlationId,
        monospace: true,
        breakAll: true,
      },
      {
        label: $localize`:Label shown in the interface:Causation event ID`,
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
      {
        label: $localize`:Label shown in the interface:Workspace`,
        value: log.workspaceSlug,
      },
      {
        label: $localize`:Label shown in the interface:Project`,
        value: log.projectSlug,
      },
      {
        label: $localize`:Label shown in the interface:Board`,
        value: log.boardSlug,
      },
    ];
  }

  protected requestProperties(
    log: AuditLogDetailViewModel
  ): readonly PropertyListItem[] {
    return [
      {
        label: $localize`:Label shown in the interface:IP address`,
        value: log.ipAddress,
      },
      {
        label: $localize`:Label shown in the interface:User agent`,
        value: log.userAgent,
        breakAll: true,
      },
    ];
  }
}
