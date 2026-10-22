import { Service, computed, inject, signal } from '@angular/core';
import {
  AiClientContext,
  AiContextChip,
  contextChipKey,
} from '@core/models/ai-context';
import { CurrentRouteService } from '@core/router/current-route.service';
import { CurrentContextService } from '@core/services/current-context.service';
import { CurrentProjectService } from '@core/services/current-project.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  boardChip,
  buildClientContext,
  pageChip,
  selectionChip,
  projectChip,
  readView,
  sprintChip,
  taskChip,
  viewChip,
} from '@core/util/ai-client-context';
import { onChange } from '@core/util/signals';

/**
 * The one place that decides what the chat knows about the screen behind it — the
 * chips above the composer and the payload sent with a message read the same list.
 */
@Service()
export class AiContextService {
  private readonly url = inject(CurrentRouteService).url;
  private readonly workspace = inject(CurrentWorkspaceService).slug;
  private readonly currentContext = inject(CurrentContextService);
  private readonly project = inject(CurrentProjectService).open;
  private readonly board = this.currentContext.board;
  private readonly sprint = this.currentContext.sprint;
  private readonly task = this.currentContext.task;
  private readonly page = this.currentContext.page;

  private readonly removed = signal<ReadonlySet<string>>(new Set());

  private readonly selection = signal<string | null>(null);

  private sentSelection: string | null = null;

  private readonly pageId = computed(() => this.page()?.id ?? null);

  constructor() {
    onChange(this.workspace, () => {
      this.restore();
      this.clearSelection();
    });

    onChange(this.pageId, () => this.clearSelection());
  }

  private readonly available = computed<AiContextChip[]>(() => {
    const workspace = this.workspace() ?? null;
    const chips: AiContextChip[] = [];

    const view = readView(this.url());
    const task = this.task();
    const board = this.board();
    const sprint = this.sprint();
    const project = this.project();
    const page = this.page();
    const selection = this.selection();

    if (view) chips.push(viewChip(view));
    if (selection) chips.push(selectionChip(selection));
    if (page) chips.push(pageChip(page, workspace));
    if (task) chips.push(taskChip(task, workspace));
    if (board) chips.push(boardChip(board, workspace));
    if (sprint) chips.push(sprintChip(sprint, workspace));
    if (project) chips.push(projectChip(project, workspace));

    return chips;
  });

  readonly chips = computed(() => {
    const removed = this.removed();

    return this.available().filter(
      (chip) => !removed.has(contextChipKey(chip))
    );
  });

  readonly hasRemoved = computed(() => {
    return this.chips().length < this.available().length;
  });

  readonly context = computed<AiClientContext | null>(() => {
    return buildClientContext(this.chips());
  });

  remove(chip: AiContextChip) {
    this.removed.update((removed) => {
      return new Set(removed).add(contextChipKey(chip));
    });
  }

  attachSelection(text: string) {
    this.selection.set(text);
  }

  contextFor(isRetry: boolean): AiClientContext | null {
    const context = this.context();
    const hasSelection = !!context?.pageSelection;
    const resendsSelection =
      isRetry && !hasSelection && this.sentSelection !== null;

    if (!resendsSelection) return context;

    return { ...context, pageSelection: this.sentSelection ?? undefined };
  }

  markSelectionSent(context: AiClientContext | null) {
    this.sentSelection = context?.pageSelection ?? null;
    this.selection.set(null);
  }

  clearSelection() {
    this.selection.set(null);
    this.sentSelection = null;
  }

  restore() {
    this.removed.set(new Set());
  }
}
