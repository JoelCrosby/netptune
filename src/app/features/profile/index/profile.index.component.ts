import { selectUpdateProfileLoading } from '@profile/store/profile.selectors';
import { AppUser } from '@core/models/appuser';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import * as AuthActions from '@core/auth/store/auth.actions';
import { AppState } from '@core/core.state';
import { Store, select } from '@ngrx/store';
import { tap, take, filter, shareReplay } from 'rxjs/operators';
import { selectProfile } from '@profile/store/profile.selectors';
import { loadProfile, updateProfile } from '@profile/store/profile.actions';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.index.component.html',
  styleUrls: ['./profile.index.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileComponent implements OnInit {
  profileGroup = new FormGroup({
    firstname: new FormControl(),
    lastname: new FormControl(),
    email: new FormControl(),
  });

  profile: AppUser;

  loadingUpdate$: Observable<boolean>;

  get firstname() {
    return this.profileGroup.get('firstname');
  }
  get lastname() {
    return this.profileGroup.get('lastname');
  }
  get email() {
    return this.profileGroup.get('email');
  }

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.store.dispatch(loadProfile());
    this.loadingUpdate$ = this.store.pipe(
      select(selectUpdateProfileLoading),
      tap((loading) =>
        loading ? this.profileGroup.disable() : this.profileGroup.enable()
      ),
      shareReplay()
    );
    this.store
      .select(selectProfile)
      .pipe(
        filter((profile) => !!profile),
        take(1),
        tap((profile) => {
          this.profile = profile;
          this.firstname.setValue(profile.firstname);
          this.lastname.setValue(profile.lastname);
          this.email.setValue(profile.email);
        })
      )
      .subscribe();
  }

  updateClicked() {
    const profile = {
      ...this.profile,
      firstname: this.firstname.value,
      lastname: this.lastname.value,
      email: this.email.value,
    };

    this.store.dispatch(updateProfile({ profile }));
  }

  onLogoutClicked() {
    this.store.dispatch(AuthActions.logout());
  }
}
