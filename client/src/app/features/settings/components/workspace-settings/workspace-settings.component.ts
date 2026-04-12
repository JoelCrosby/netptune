import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';

@Component({
  selector: 'app-workspace-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FlatButtonComponent],
  template: `<h3 class="font-overpass text-[1.4rem] font-normal">Workspace</h3>

    <div class="mt-4 flex flex-col items-start gap-4">
      <button app-flat-button>Mark Workspace as Public</button>
    </div>`,
})
export class WorkspaceSettings {}
