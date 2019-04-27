import { ActionAuthLogout } from '@app/core/auth/store/auth.actions';
import { AppState } from '@app/core/core.state';
import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.index.component.html',
  styleUrls: ['./profile.index.component.scss'],
})
export class ProfileComponent {
  constructor(private store: Store<AppState>) {}

  onLogoutClicked = () => this.store.dispatch(new ActionAuthLogout());
}
