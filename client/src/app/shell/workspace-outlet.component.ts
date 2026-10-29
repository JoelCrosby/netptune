import { Component, computed, inject, untracked } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { REBUILT_ON_WORKSPACE_SWITCH } from '@core/util/reload-on-refresh';
import { map } from 'rxjs';

// Rebuilds the routed views on a switch but not on a rename, so they reload as new
// views rather than each reloading in place. The id is read when the route param
// changes, by which point the guard has opened the new workspace.
@Component({
  selector: 'app-workspace-outlet',
  imports: [RouterOutlet],
  providers: [{ provide: REBUILT_ON_WORKSPACE_SWITCH, useValue: true }],
  host: { class: 'contents' },
  template: `
    @for (id of workspaceKey(); track id) {
      <router-outlet />
    }
  `,
})
export class WorkspaceOutletComponent {
  private readonly workspaceSlug = toSignal(
    inject(ActivatedRoute).paramMap.pipe(
      map((params) => params.get('workspace'))
    )
  );

  private readonly workspaceId = inject(CurrentWorkspaceService).id;

  protected readonly workspaceKey = computed(() => {
    this.workspaceSlug();

    return [untracked(this.workspaceId)];
  });
}
