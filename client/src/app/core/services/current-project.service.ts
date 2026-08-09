import { computed, Injectable } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';

/** Always the workspace's first project — nothing in the app lets the user pick another. */
@Injectable({ providedIn: 'root' })
export class CurrentProjectService {
  private readonly projects = projectResource();

  readonly current = computed(() => this.projects.value()[0]);
  readonly currentId = computed(() => this.current()?.id);
}
