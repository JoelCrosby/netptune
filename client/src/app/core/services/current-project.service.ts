import { computed, Injectable } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';

/**
 * The project a view falls back to when nothing else names one. It has always been
 * the workspace's first project — nothing in the app lets the user pick another.
 */
@Injectable({ providedIn: 'root' })
export class CurrentProjectService {
  private readonly projects = projectResource();

  readonly current = computed(() => this.projects.value()[0]);
  readonly currentId = computed(() => this.current()?.id);
}
