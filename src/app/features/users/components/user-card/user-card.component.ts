import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { AppUser } from '@app/core/models/appuser';
import { Store } from '@ngrx/store';
import { removeUsersFromWorkspace } from '@core/store/users/users.actions';

@Component({
  selector: 'app-user-card',
  templateUrl: './user-card.component.html',
  styleUrls: ['./user-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UserCardComponent {
  @Input() user: AppUser;

  constructor(private store: Store) {}

  onRemoveClicked() {
    if (!this.user) return;

    const emailAddresses = [this.user.email];
    this.store.dispatch(removeUsersFromWorkspace({ emailAddresses }));
  }
}
