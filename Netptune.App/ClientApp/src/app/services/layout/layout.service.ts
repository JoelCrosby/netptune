import { Injectable } from '@angular/core';
import { WorkspaceService } from '../workspace/workspace.service';
import { Workspace } from '@app/models/workspace';
import { AuthGuardService } from '../auth/auth-guard.service';

@Injectable({
  providedIn: 'root'
})
export class LayoutService {

  showSidebar = false;

  constructor(
    private workspaceService: WorkspaceService,
    private authGuardService: AuthGuardService
  ) {
    this.workspaceService.onWorkspaceChanged.subscribe(
      (workspace: Workspace) => {
        this.showSidebar = workspace ? true : false;
      }
    );
    this.authGuardService.onNotAuthenticated.subscribe(() => {
      this.showSidebar = false;
    });
  }
}
