import { computed, inject, Service } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';
import { CurrentContextService } from '@core/services/current-context.service';

@Service()
export class CurrentProjectService {
  private readonly projects = projectResource();
  private readonly currentContext = inject(CurrentContextService);

  // The project of whatever is open, most specific first. Absent when nothing open
  // belongs to a project, so the assistant is not told about one the user never chose.
  readonly open = computed(() => {
    const projectId =
      this.currentContext.task()?.projectId ??
      this.currentContext.board()?.projectId ??
      this.currentContext.sprint()?.projectId;

    if (projectId === undefined) {
      return undefined;
    }

    return this.projects.value().find((project) => project.id === projectId);
  });

  // A default for forms that need some project, even when none is open.
  readonly current = computed(() => this.open() ?? this.projects.value()[0]);

  readonly currentId = computed(() => this.current()?.id);
}
