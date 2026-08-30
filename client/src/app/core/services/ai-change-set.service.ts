import { Service, inject, signal } from '@angular/core';
import { LocalStorageService } from '@core/local-storage/local-storage.service';
import {
  AiChangeSet,
  AiChangeSetStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';
import {
  AiApiService,
  AiChangeFieldEdit,
  AiChangeSetAction,
} from '@core/services/ai-api.service';
import { AiApplyProgressService } from '@core/services/ai-apply-progress.service';
import { AiStreamService } from '@core/services/ai-stream.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import {
  landedChanges,
  refreshScopesForChanges,
} from '@core/util/ai-refresh-scopes';

const EXPANDED_STORAGE_KEY = 'ai-assistant.applied-changes-expanded';

@Service()
export class AiChangeSetService {
  private readonly api = inject(AiApiService);
  private readonly storage = inject(LocalStorageService);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly stream = inject(AiStreamService);
  private readonly progress = inject(AiApplyProgressService);

  readonly changeSet = signal<AiChangeSet | null>(null);
  readonly excludedChangeIds = signal<Set<number>>(new Set());
  readonly isApplying = signal(false);
  readonly isEditing = signal(false);

  /**
   * Applying collapses the review into a one line summary, so what landed stays behind an
   * expander. Each set keeps its own state, which is what lets an earlier one further up the
   * conversation stay open, and the last choice becomes the default for the sets after it.
   */
  private readonly expandedByDefault = signal(
    this.storage.getItem<boolean>(EXPANDED_STORAGE_KEY) ?? false
  );
  private readonly expandedById = signal<ReadonlyMap<string, boolean>>(
    new Map()
  );

  isExpanded(changeSetId: string): boolean {
    return this.expandedById().get(changeSetId) ?? this.expandedByDefault();
  }

  toggleExpanded(changeSetId: string) {
    const isExpanded = !this.isExpanded(changeSetId);

    this.expandedById.update((current) => {
      return new Map(current).set(changeSetId, isExpanded);
    });

    this.expandedByDefault.set(isExpanded);
    this.storage.setItem(EXPANDED_STORAGE_KEY, isExpanded);
  }

  set(changeSet: AiChangeSet | null) {
    this.changeSet.set(changeSet);
    this.excludedChangeIds.set(new Set());
  }

  clear() {
    this.set(null);
  }

  toggleChanges(changeIds: number[]) {
    for (const changeId of changeIds) {
      this.toggleChange(changeId);
    }
  }

  toggleChange(changeId: number) {
    this.excludedChangeIds.update((current) => {
      const next = new Set(current);
      const wasExcluded = next.has(changeId);

      if (wasExcluded) {
        next.delete(changeId);
      } else {
        next.add(changeId);
      }

      return next;
    });
  }

  /**
   * The proposal and the values that will be applied are two halves of one record on the
   * server, so an edit comes back as the whole change set rather than a patched change.
   */
  async updateChange(
    changeId: number,
    fields: AiChangeFieldEdit[]
  ): Promise<string | null> {
    const changeSet = this.changeSet();

    if (!changeSet || this.isEditing()) {
      return null;
    }

    this.isEditing.set(true);

    try {
      const result = await this.api.updateChange(changeSet.id, changeId, fields);

      if (result.changeSet) {
        this.changeSet.set(result.changeSet);
      }

      return result.error;
    } finally {
      this.isEditing.set(false);
    }
  }

  async apply() {
    const changeSet = this.changeSet();

    if (!changeSet || this.isApplying()) {
      return;
    }

    const excluded = this.excludedChangeIds();
    const changeIds = changeSet.changes
      .filter((change) => !excluded.has(change.id))
      .map((change) => change.id);

    if (changeIds.length === 0) {
      return;
    }

    this.isApplying.set(true);
    this.progress.start(changeSet.id, changeIds.length);

    try {
      await this.stream.runApply(
        { changeSetId: changeSet.id, changeIds },
        (event) => {
          this.progress.receive(event);
        }
      );
    } catch {
      /* A dropped stream loses the reporting, not the run; the re-read below says what landed. */
    } finally {
      const wasRead = await this.refresh(changeSet.id);

      this.refreshAffectedViews(changeSet.changes, wasRead);
      this.progress.reset();
      this.isApplying.set(false);
    }
  }

  /**
   * The run keeps going until the server hears about the stop, so the request stays open —
   * abandoning it here would leave the outcome of what already landed unread.
   */
  async stopApply() {
    const changeSet = this.changeSet();

    if (!changeSet || !this.isApplying() || this.progress.isStopping()) {
      return;
    }

    this.progress.markStopping();

    await this.api.stopChangeSetApply(changeSet.id);
  }

  async retryFailed() {
    const changeSet = this.changeSet();

    if (!changeSet || this.isApplying()) {
      return;
    }

    await this.run(changeSet, 'retry');
  }

  async undo() {
    const changeSet = this.changeSet();

    if (!changeSet || this.isApplying()) {
      return;
    }

    await this.run(changeSet, 'undo');
  }

  async discard() {
    const changeSet = this.changeSet();

    if (!changeSet) {
      return;
    }

    await this.api.runChangeSetAction(changeSet.id, 'discard');
    await this.refresh(changeSet.id);
  }

  async refresh(
    changeSetId: string,
    isCurrent?: () => boolean
  ): Promise<boolean> {
    const payload = await this.api.readChangeSet(changeSetId);
    const isStale = isCurrent !== undefined && !isCurrent();

    if (payload === null || isStale) {
      return false;
    }

    this.changeSet.set(payload);

    return true;
  }

  /**
   * A proposal reaches the client through a single event at the end of a turn,
   * so a dropped connection or a failed read leaves a stored change set with
   * nothing on screen pointing at it. Ask for it directly rather than lose it.
   */
  async recoverPending(conversationId: string, isCurrent: () => boolean) {
    if (this.hasPending()) {
      return;
    }

    const pending = await this.api.readPendingChangeSet(conversationId);
    const canRecover = pending !== null && isCurrent() && !this.hasPending();

    if (!canRecover) {
      return;
    }

    this.set(pending);
  }

  hasPending(): boolean {
    return this.changeSet()?.status === AiChangeSetStatus.pending;
  }

  private async run(
    changeSet: AiChangeSet,
    action: AiChangeSetAction,
    body: object = {}
  ) {
    this.isApplying.set(true);

    try {
      await this.api.runChangeSetAction(changeSet.id, action, body);

      const wasRead = await this.refresh(changeSet.id);

      this.refreshAffectedViews(changeSet.changes, wasRead);
    } finally {
      this.isApplying.set(false);
    }
  }

  /* Without a re-read there is nothing to diff, so every proposal counts as landed. */
  private refreshAffectedViews(
    before: readonly AiProposedChange[],
    wasRead: boolean
  ) {
    const after = this.changeSet()?.changes ?? [];
    const landed = wasRead ? landedChanges(before, after) : before;

    this.workspaceRefresh.refresh(refreshScopesForChanges(landed));
  }
}
