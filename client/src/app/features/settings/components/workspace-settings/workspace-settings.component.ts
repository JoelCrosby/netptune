import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormToggleComponent } from '@static/components/form-toggle/form-toggle.component';

@Component({
  selector: 'app-workspace-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormToggleComponent],
  template: `<h3 class="font-overpass text-[1.4rem] font-normal">Workspace</h3>

    <app-form-toggle label="Public" /> `,
})
export class WorkspaceSettings {}
