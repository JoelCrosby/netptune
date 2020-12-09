import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { WorkspaceAppUser } from '@core/models/appuser';

@Component({
  selector: 'app-user-list-item',
  templateUrl: './user-list-item.component.html',
  styleUrls: ['./user-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserListItemComponent {
  @Input() user: WorkspaceAppUser;
}
