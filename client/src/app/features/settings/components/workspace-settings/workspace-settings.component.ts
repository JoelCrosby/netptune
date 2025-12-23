import { ChangeDetectionStrategy, Component } from '@angular/core';
import { FormToggleComponent } from '@static/components/form-toggle/form-toggle.component';

@Component({
  selector: 'app-workspace-settings',
  templateUrl: './workspace-settings.component.html',
  styleUrl: './workspace-settings.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormToggleComponent],
})
export class WorkspaceSettings {}
