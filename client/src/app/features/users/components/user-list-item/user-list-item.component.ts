import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';
import { MatCheckbox } from '@angular/material/checkbox';
import { AvatarComponent } from '@static/components/avatar/avatar.component';

@Component({
  selector: 'app-user-list-item',
  templateUrl: './user-list-item.component.html',
  styleUrls: ['./user-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatCheckbox, AvatarComponent],
})
export class UserListItemComponent {
  readonly user = input.required<WorkspaceAppUser>();
}
