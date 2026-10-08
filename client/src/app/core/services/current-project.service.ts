import { computed, inject, Service } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';
import { CurrentBoardService } from '@core/services/current-board.service';
import { CurrentSprintService } from '@core/services/current-sprint.service';
import { CurrentTaskService } from '@core/services/current-task.service';

@Service()
export class CurrentProjectService {
  private readonly projects = projectResource();
  private readonly task = inject(CurrentTaskService).task;
  private readonly board = inject(CurrentBoardService).board;
  private readonly sprint = inject(CurrentSprintService).sprint;

  // The project of whatever is open, most specific first. Absent when nothing open
  // belongs to a project, so the assistant is not told about one the user never chose.
  readonly open = computed(() => {
    const projectId =
      this.task()?.projectId ??
      this.board()?.projectId ??
      this.sprint()?.projectId;

    if (projectId === undefined) {
      return undefined;
    }

    return this.projects.value().find((project) => project.id === projectId);
  });

  // A default for forms that need some project, even when none is open.
  readonly current = computed(() => this.open() ?? this.projects.value()[0]);

  readonly currentId = computed(() => this.current()?.id);
}
