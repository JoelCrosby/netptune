import { Component, OnInit } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { ActionAuthLogout } from '@app/core/auth/store/auth.actions';
import { AppState } from '@app/core/core.state';
import { Store } from '@ngrx/store';
import { filter, tap } from 'rxjs/operators';
import { selectProfile } from '../store/profile.selectors';
import { ActionLoadProfile } from './../store/profile.actions';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.index.component.html',
  styleUrls: ['./profile.index.component.scss'],
})
export class ProfileComponent implements OnInit {
  profileGroup = new FormGroup({
    firstname: new FormControl(),
    lastname: new FormControl(),
    email: new FormControl(),
  });

  constructor(private store: Store<AppState>) {
    this.store
      .select(selectProfile)
      .pipe(
        filter(profile => profile !== null),
        tap(profile => {
          this.profileGroup.get('firstname').setValue(profile.firstName);
          this.profileGroup.get('lastname').setValue(profile.lastName);
          this.profileGroup.get('email').setValue(profile.email);
        })
      )
      .subscribe();
  }

  onLogoutClicked = () => this.store.dispatch(new ActionAuthLogout());

  ngOnInit() {
    this.store.dispatch(new ActionLoadProfile());
  }
}
