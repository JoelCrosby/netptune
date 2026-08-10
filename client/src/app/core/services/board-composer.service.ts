import { computed, inject, Service, linkedSignal } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { BoardViewService } from '@core/services/board-view.service';
import { TaskFilterService } from '@core/services/task-filter.service';

@Service()
export class BoardComposerService {
  private readonly boardView = inject(BoardViewService);
  private readonly filters = inject(TaskFilterService);

  private readonly currentUserId = inject(SessionService).currentUserId;

  private readonly openGroupId = linkedSignal<
    string | undefined,
    number | undefined
  >({
    source: this.boardView.identifier,
    computation: () => undefined,
  });

  private readonly draft = linkedSignal<
    string | undefined,
    string | null | undefined
  >({
    source: this.boardView.identifier,
    computation: () => undefined,
  });

  private readonly submitted = linkedSignal<string | undefined, boolean>({
    source: this.boardView.identifier,
    computation: () => false,
  });

  readonly activeGroupId = this.openGroupId.asReadonly();
  readonly content = this.draft.asReadonly();
  readonly isDirty = this.submitted.asReadonly();

  readonly sprintId = computed(() => this.filters.filters().sprintId);

  readonly assigneeId = computed(() => {
    const users = this.filters.filters().users;

    if (users?.length === 1) return users[0];

    return this.currentUserId();
  });

  readonly warning = computed(() => {
    const filters = this.filters.filters();
    const differentUser = this.currentUserId() !== this.assigneeId();
    const filterApplied = !!filters.term || !!filters.tags?.length;

    if (!differentUser && !filterApplied) return null;

    return $localize`:Warning shown above the box for creating a task on a filtered board:The filters currently applied may cause the newly created task to be hidden.`;
  });

  open(groupId: number) {
    this.openGroupId.set(groupId);
  }

  close() {
    this.openGroupId.set(undefined);
  }

  setContent(content: string | null | undefined) {
    this.draft.set(content);
  }

  setIsDirty(isDirty: boolean) {
    this.submitted.set(isDirty);
  }
}
