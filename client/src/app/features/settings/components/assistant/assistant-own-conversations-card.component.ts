import { Component, computed, inject, signal, viewChild } from '@angular/core';
import { Params } from '@angular/router';
import { AiConversation } from '@core/models/ai-conversation';
import { AiAssistantService } from '@core/services/ai-assistant.service';
import { AiPanelService } from '@core/services/ai-panel.service';
import { formatCost, formatTokens } from '@core/util/ai-usage';
import { reloadToken } from '@core/util/signals';
import { LucideMessagesSquare, LucideTrash2 } from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableMenuItem,
  DatatableRemoteDataSource,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { PanelComponent } from '@static/components/panel.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  buildConversationsDeletedMessage,
  DELETE_CONVERSATIONS_FAILED,
} from '@settings/components/assistant/assistant-conversation-messages';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';

@Component({
  selector: 'app-assistant-own-conversations-card',
  imports: [
    DatatableCellTemplateDirective,
    DatatableComponent,
    DatatableEmptyDirective,
    EmptyStateComponent,
    LucideMessagesSquare,
    LucideTrash2,
    PanelComponent,
    PanelHeaderComponent,
    PrettyDatePipe,
    StrokedButtonComponent,
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
          Explains the list of the signed-in user's own assistant conversations
        "
        description="Your conversations with the assistant in this workspace." />

      @if (selectedCount() > 0) {
        <div class="flex h-12 items-center justify-end gap-4 px-4">
          <span
            class="text-muted text-sm"
            i18n="
              Count of selected rows above a table. COUNT is the number selected
            ">
            {{
              selectedCount() // i18n(ph="COUNT")
            }}
            selected
          </span>
          <button
            app-stroked-button
            type="button"
            color="warn"
            (click)="deleteSelected()">
            <svg lucideTrash2 class="h-4 w-4"></svg>
            <span
              i18n="Button that deletes the selected assistant conversations">
              Delete
            </span>
          </button>
        </div>
      }

      <app-datatable
        containerClass="border-0"
        tableClass="table-fixed"
        i18n-itemLabel="
          Plural noun for assistant conversations, used in the row summary
        "
        i18n-errorMessage="
          Shown when the assistant conversation list fails to load
        "
        errorMessage="Conversations could not be loaded."
        itemLabel="conversations"
        selection
        [rounded]="false"
        [skeletonRows]="5"
        [defaultPageSize]="25"
        [data]="data"
        [(sort)]="sort"
        (selectionChanged)="selection.set($event)">
        <ng-template appDatatableCell="title" let-conversation>
          <button
            type="button"
            class="block w-full truncate text-left font-medium hover:underline"
            (click)="open(conversation)">
            {{ conversation.title }}
          </button>
        </ng-template>

        <ng-template appDatatableCell="lastMessageAt" let-conversation>
          <span class="whitespace-nowrap">
            {{ toDate(conversation.lastMessageAt) | prettyDate }}
          </span>
        </ng-template>

        <ng-template appDatatableEmpty>
          <app-empty-state
            compact
            i18n-title="
              Heading when the signed-in user has no assistant conversations
            "
            title="You have no conversations"
            i18n-description="
              Explains why the signed-in user's assistant conversation list is
              empty
            "
            description="Conversations appear here once you use the assistant">
            <svg emptyStateIcon lucideMessagesSquare class="h-8 w-8"></svg>
          </app-empty-state>
        </ng-template>
      </app-datatable>
    </section>
  `,
})
export class AssistantOwnConversationsCardComponent {
  private readonly assistant = inject(AiAssistantService);
  private readonly panel = inject(AiPanelService);
  private readonly snackbar = inject(SnackbarService);

  private readonly table = viewChild(DatatableComponent);

  protected readonly conversationIcon = LucideMessagesSquare;
  protected readonly sort = signal<DatatableSort | null>(null);

  protected readonly selection = signal<AiConversation[]>([]);
  protected readonly selectedCount = computed(() => this.selection().length);

  private readonly reloadVersion = reloadToken();

  // The endpoint takes no filters, so the table only ever varies its own paging
  // and sort parameters.
  private readonly params = signal<Params>({});

  private readonly menu: DatatableMenuItem<AiConversation>[] = [
    {
      label: $localize`:Row action that deletes an assistant conversation:Delete`,
      icon: LucideTrash2,
      onClick: (conversation) => void this.delete([conversation]),
    },
  ];

  protected readonly data: DatatableRemoteDataSource<AiConversation> = {
    key: 'personal-assistant-conversations',
    columns: [
      {
        id: 'title',
        header: $localize`:Column heading for an assistant conversation:Conversation`,
        visibleOnMobile: true,
        accessor: 'title',
        sortable: true,
        cellClass: 'overflow-hidden',
      },
      {
        id: 'messageCount',
        header: $localize`:Column heading for the number of messages in a conversation:Messages`,
        visibleOnMobile: false,
        accessor: 'messageCount',
        sortable: true,
        align: 'end',
        widthClass: 'w-28',
        cellClass: 'text-muted',
      },
      {
        id: 'tokens',
        header: $localize`:Column heading for the tokens a conversation used:Tokens`,
        visibleOnMobile: false,
        accessor: (conversation) => formatTokens(conversation.usage),
        sortable: true,
        align: 'end',
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
      {
        id: 'cost',
        header: $localize`:Column heading for what a conversation cost:Cost`,
        visibleOnMobile: false,
        accessor: (conversation) => formatCost(conversation.usage),
        align: 'end',
        widthClass: 'w-24',
        cellClass: 'text-muted',
      },
      {
        id: 'lastMessageAt',
        header: $localize`:Column heading for when a conversation was last active:Last message`,
        visibleOnMobile: true,
        sortable: true,
        align: 'end',
        widthClass: 'w-56',
        cellClass: 'text-muted',
      },
    ],
    resource: {
      url: 'api/ai/conversations',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    reloadSignal: this.reloadVersion,
    trackBy: (_: number, conversation: AiConversation) => conversation.id,
    menu: this.menu,
  };

  protected open(conversation: AiConversation) {
    this.panel.open();
    void this.assistant.openConversation(conversation.id);
  }

  protected deleteSelected() {
    void this.delete(this.selection());
  }

  private async delete(conversations: readonly AiConversation[]) {
    const deletedIds = await this.assistant.deleteConversations(conversations);

    if (!deletedIds) return;

    this.table()?.clearSelection();
    this.selection.set([]);
    this.reloadVersion.bump();

    const hasFailures = deletedIds.length < conversations.length;

    if (hasFailures) {
      this.snackbar.error(DELETE_CONVERSATIONS_FAILED);

      return;
    }

    this.snackbar.open(buildConversationsDeletedMessage(deletedIds.length));
  }

  protected toDate(value: string): Date {
    return new Date(value);
  }
}
