import { Injectable, computed, inject, signal } from '@angular/core';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { WorkspaceEventsService } from '@core/sse/workspace-events.service';

@Injectable({
  providedIn: 'root',
})
export class ProjectTasksHubService {
  private workspaceEvents = inject(WorkspaceEventsService);
  private workspaceRefresh = inject(WorkspaceRefreshService);

  private readonly localReloads = signal(0);

  readonly onlineUserIds = this.workspaceEvents.onlineUserIds;

  readonly updateVersion = computed(() => {
    return this.localReloads() + this.workspaceRefresh.version('tasks')();
  });

  addToGroup(groupId: string) {
    this.workspaceEvents.joinGroup(groupId);
  }

  leaveGroup() {
    this.workspaceEvents.leaveGroup();
  }

  reloadTaskList() {
    this.localReloads.update((version) => version + 1);
  }
}
