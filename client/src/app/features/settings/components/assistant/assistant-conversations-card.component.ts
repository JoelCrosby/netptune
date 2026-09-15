import { HttpClient } from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { Params } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { AiWorkspaceConversation } from '@core/models/ai-workspace-conversation';
import { ClientResponse } from '@core/models/client-response';
import { ConfirmationService } from '@core/services/confirmation.service';
import { formatCost, formatTokens } from '@core/util/ai-usage';
import { getErrorMessage } from '@core/util/error-message';
import { requireSuccess } from '@core/util/rxjs-operators';
import { reloadToken } from '@core/util/signals';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { LucideMessagesSquare, LucideTrash2 } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableDataSource,
  DatatableMenuItem,
  DatatableSort,
} from '@static/components/datatable/datatable.types';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PrettyDatePipe } from '@static/pipes/pretty-date.pipe';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { EMPTY, switchMap } from 'rxjs';

@Component({
  selector: 'app-assistant-conversations-card',
  imports: [
    AvatarComponent,
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
          Explains what an admin sees on the assistant conversations page
        "
        description="What members asked the assistant. The record of what changed lives in the audit log." />

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
        [data]="data()"
        [selection]="canDelete()"
        [(sort)]="sort"
        (selectionChanged)="selection.set($event)">
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
  private readonly http = inject(HttpClient);
  private readonly confirmation = inject(ConfirmationService);
  private readonly snackbar = inject(SnackbarService);

  private readonly table = viewChild(DatatableComponent);

  readonly opened = output<AiWorkspaceConversation>();
  readonly deleted = output();

  protected readonly conversationIcon = LucideMessagesSquare;
  protected readonly sort = signal<DatatableSort | null>(null);

  protected readonly canDelete = hasPermission(
    PERMISSIONS.assistant.deleteAnyConversations
  );

  protected readonly selection = signal<AiWorkspaceConversation[]>([]);
  protected readonly selectedCount = computed(() => this.selection().length);

  private readonly reloadVersion = reloadToken();

  private readonly menu: DatatableMenuItem<AiWorkspaceConversation>[] = [
    {
      label: $localize`:Row action that deletes an assistant conversation:Delete`,
      icon: LucideTrash2,
      onClick: (conversation) => this.delete([conversation]),
    },
  ];

  // The endpoint takes no filters, so the table only ever varies its own paging
  // and sort parameters.
  private readonly params = signal<Params>({});

  private readonly source: DatatableDataSource<AiWorkspaceConversation> = {
    key: 'workspace-assistant-conversations',
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
        id: 'user',
        header: $localize`:Column heading for the member who held a conversation:Member`,
        visibleOnMobile: false,
        accessor: 'userDisplayName',
        sortable: true,
        widthClass: 'w-56',
        cellClass: 'text-muted overflow-hidden',
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
      url: 'api/ai/admin/conversations',
      params: this.params,
    },
    rows: (response) => response?.payload?.items ?? [],
    trackBy: (_: number, conversation: AiWorkspaceConversation) =>
      conversation.id,
    reloadSignal: this.reloadVersion,
  };

  protected readonly data = computed(() => {
    return { ...this.source, menu: this.canDelete() ? this.menu : undefined };
  });

  protected deleteSelected() {
    this.delete(this.selection());
  }

  private delete(conversations: readonly AiWorkspaceConversation[]) {
    if (conversations.length === 0) return;

    const ids = conversations.map((conversation) => conversation.id);

    this.confirmation
      .open(buildDeleteConfirmation(conversations))
      .pipe(
        switchMap((confirmed) => {
          if (!confirmed) return EMPTY;

          return this.http
            .delete<ClientResponse>('api/ai/admin/conversations', {
              body: ids,
            })
            .pipe(requireSuccess());
        })
      )
      .subscribe({
        next: () => {
          this.snackbar.open(buildDeletedMessage(ids.length));
          this.table()?.clearSelection();
          this.selection.set([]);
          this.reloadVersion.bump();
          this.deleted.emit();
        },
        error: (error: unknown) => {
          this.snackbar.error(getErrorMessage(error, DELETE_FAILED));
        },
      });
  }

  protected toDate(value: string): Date {
    return new Date(value);
  }
}

const DELETE_FAILED = $localize`:Error shown after an action fails:The conversation(s) could not be deleted. Please try again.`;

function buildDeletedMessage(count: number): string {
  if (count === 1) {
    return $localize`:Confirmation shown after an assistant conversation is deleted:Conversation deleted`;
  }

  return $localize`:Confirmation shown after assistant conversations are deleted. COUNT is how many:${count}:COUNT: conversations deleted`;
}

function buildDeleteConfirmation(
  conversations: readonly AiWorkspaceConversation[]
): ConfirmDialogOptions {
  const owners = [
    ...new Set(
      conversations.map((conversation) => conversation.userDisplayName)
    ),
  ].join(', ');
  const acceptLabel = $localize`:Confirms deleting assistant conversations in a dialog:Delete`;

  if (conversations.length === 1) {
    const title = conversations[0].title;

    return {
      acceptLabel,
      color: 'warn',
      title: $localize`:Title of the dialog that deletes one assistant conversation:Delete conversation`,
      message: $localize`:Warns that deleting a member's assistant conversation removes it for them too. TITLE is the conversation title, OWNER the member who held it:"${title}:TITLE:" belongs to ${owners}:OWNER:. Deleting it removes it from this list and from their own assistant history.`,
    };
  }

  const count = conversations.length;

  return {
    acceptLabel,
    color: 'warn',
    title: $localize`:Title of the dialog that deletes several assistant conversations. COUNT is how many:Delete ${count}:COUNT: conversations`,
    message: $localize`:Warns that deleting members' assistant conversations removes them for those members too. OWNERS lists the members who held them:These conversations belong to ${owners}:OWNERS:. Deleting them removes them from this list and from each member's own assistant history.`,
  };
}
