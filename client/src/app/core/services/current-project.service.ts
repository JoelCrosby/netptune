import { computed, inject, Service } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';
import { CurrentBoardService } from '@core/services/current-board.service';

@Service()
export class CurrentProjectService {
  private readonly projects = projectResource();
  private readonly board = inject(CurrentBoardService).board;

  readonly current = computed(() => {
    const projects = this.projects.value();
    const board = this.board();

    if (board === undefined) {
      return projects[0];
    }

    const match = projects.find((project) => project.id === board.projectId);

    return match ?? projects[0];
  });

  readonly currentId = computed(() => this.current()?.id);
}
