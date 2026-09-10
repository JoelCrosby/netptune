import { Component, output, signal } from '@angular/core';
import { Params } from '@angular/router';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { formatCost, formatTokens } from '@core/util/ai-usage';
import { LucideMessagesSquare } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';

@Component({
  selector: 'app-assistant-conversations-card',
  imports: [
    AvatarComponent,
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    LucideMessagesSquare,
    PanelComponent,
    PanelHeaderComponent,
    PrettyDatePipe,
  ],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-header
        density="comfortable"
        [icon]="conversationIcon"
        i18n-heading="Heading of the assistant conversation list"
        heading="Conversations"
        i18n-description="
          Explains what an admin sees on the assistant conversations page
        "
        description="What members asked the assistant. The record of what changed lives in the audit log." />

      <app-datatable
        containerClass="border-0"
        tableClass="table-fixed"
        i18n-errorMessage="
          Shown when the assistant conversation list fails to load
        "
        errorMessage="Conversations could not be loaded."
        i18n-itemLabel="
          Plural noun for assistant conversations, used in the row summary
        "
        itemLabel="conversations"
        [rounded]="false"
        [skeletonRows]="5"
        [defaultPageSize]="25"
        [data]="data"
        [(sort)]="sort">
        <ng-template appDatatableCell="title" let-conversation>
          <button
            type="button"
            class="block w-full truncate text-left font-medium hover:underline"
            (click)="opened.emit(conversation)">
            {{ conversation.title }}
          </button>
        </ng-template>

        <ng-template appDatatableCell="user" let-conversation>
          <div class="flex min-w-0 items-center gap-2">
            <app-avatar
              class="shrink-0"
              size="sm"
              [name]="conversation.userDisplayName"
              [imageUrl]="conversation.userPictureUrl" />
            <span class="min-w-0 truncate">
              {{ conversation.userDisplayName }}
            </span>
          </div>
        </ng-template>

        <ng-template appDatatableCell="lastMessageAt" let-conversation>
          <span class="whitespace-nowrap">
            {{ toDate(conversation.lastMessageAt) | prettyDate }}
          </span>
        </ng-template>

        <ng-template appDatatableEmpty>
          <app-empty-state
            compact
            i18n-title="Heading when no assistant conversations exist"
            title="There are no conversations"
            i18n-description="
              Explains why the assistant conversation list is empty
            "
            description="Conversations appear here once members use the assistant">
            <svg emptyStateIcon lucideMessagesSquare class="h-8 w-8"></svg>
          </app-empty-state>
        </ng-template>
      </app-datatable>
    </section>
  `,
})
export class AssistantConversationsCardComponent {
  readonly opened = output<AiWorkspaceConversation>();

  protected readonly conversationIcon = LucideMessagesSquare;
  protected readonly sort = signal<DatatableSort | null>(null);

  // The endpoint takes no filters, so the table only ever varies its own paging
  // and sort parameters.
  private readonly params = signal<Params>({});

  protected readonly data: DatatableDataSource<AiWorkspaceConversation> = {
    key: 'workspace-assistant-conversations',
    columns: [
      {
        id: 'title',
        header: $localize`:Column heading for an assistant conversation:Conversation`,
        accessor: 'title',
        sortable: true,
        cellClass: 'overflow-hidden',
      },
      {
        id: 'user',
        header: $localize`:Column heading for the member who held a conversation:Member`,
        accessor: 'userDisplayName',
        sortable: true,
        widthClass: 'w-56',
        cellClass: 'text-muted overflow-hidden',
      },
      {
        id: 'messageCount',
        header: $localize`:Column heading for the number of messages in a conversation:Messages`,
        accessor: 'messageCount',
        sortable: true,
        align: 'end',
        widthClass: 'w-28',
        cellClass: 'text-muted',
      },
      {
        id: 'tokens',
        header: $localize`:Column heading for the tokens a conversation used:Tokens`,
        accessor: (conversation) => formatTokens(conversation.usage),
        sortable: true,
        align: 'end',
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
      {
        id: 'cost',
        header: $localize`:Column heading for what a conversation cost:Cost`,
        accessor: (conversation) => formatCost(conversation.usage),
        align: 'end',
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
      {
        id: 'lastMessageAt',
        header: $localize`:Column heading for when a conversation was last active:Last message`,
        sortable: true,
        align: 'end',
        widthClass: 'w-56',
        cellClass: 'text-muted',
      },
    ],
    resource: {
      url: 'api/ai/admin/conversations',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, conversation: AiWorkspaceConversation) =>
      conversation.id,
  };

  protected toDate(value: string): Date {
    return new Date(value);
  }
}
