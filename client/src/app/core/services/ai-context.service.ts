import { Service, computed, inject, signal } from '@angular/core';
import {
  AiClientContext,
  AiContextChip,
  contextChipKey,
} from '@core/models/ai-context';
import { CurrentRouteService } from '@core/router/current-route.service';
import { CurrentBoardService } from '@core/services/current-board.service';
import { CurrentProjectService } from '@core/services/current-project.service';
import { CurrentSprintService } from '@core/services/current-sprint.service';
import { CurrentTaskService } from '@core/services/current-task.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  boardChip,
  buildClientContext,
  projectChip,
  readView,
  sprintChip,
  taskChip,
  viewChip,
} from '@core/util/ai-client-context';

/**
 * The one place that decides what the chat knows about the screen behind it — the
 * chips above the composer and the payload sent with a message read the same list.
 */
@Service()
export class AiContextService {
  private readonly url = inject(CurrentRouteService).url;
  private readonly workspace = inject(CurrentWorkspaceService).slug;
  private readonly project = inject(CurrentProjectService).current;
  private readonly board = inject(CurrentBoardService).board;
  private readonly sprint = inject(CurrentSprintService).sprint;
  private readonly task = inject(CurrentTaskService).task;

  private readonly removed = signal<ReadonlySet<string>>(new Set());

  private readonly available = computed<AiContextChip[]>(() => {
    const workspace = this.workspace() ?? null;
    const chips: AiContextChip[] = [];

    const view = readView(this.url());
    const task = this.task();
    const board = this.board();
    const sprint = this.sprint();
    const project = this.project();

    if (view) chips.push(viewChip(view));
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

  restore() {
    this.removed.set(new Set());
  }
}
