import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import { FormControl, FormGroup } from '@angular/forms';
import { select, Store } from '@ngrx/store';
import { loadProfile, updateProfile } from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable } from 'rxjs';
import { filter, first, shareReplay, tap } from 'rxjs/operators';

@Component({
  selector: 'app-update-profile',
  templateUrl: './update-profile.component.html',
  styleUrls: ['./update-profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdateProfileComponent implements OnInit, AfterViewInit {
  profileGroup = new FormGroup({
    firstname: new FormControl(),
    lastname: new FormControl(),
    email: new FormControl(),
    pictureUrl: new FormControl(),
  });

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
  get pictureUrl() {
    return this.profileGroup.get('pictureUrl');
  }

  constructor(private store: Store) {}

  ngOnInit() {
    this.loadingUpdate$ = this.store.pipe(
      select(ProfileSelectors.selectUpdateProfileLoading),
      tap((loading) =>
        loading ? this.profileGroup.disable() : this.profileGroup.enable()
      ),
      shareReplay()
    );

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          this.firstname.setValue(profile.firstname);
          this.lastname.setValue(profile.lastname);
          this.email.setValue(profile.email);
          this.pictureUrl.setValue(profile.pictureUrl);
        })
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProfile());
  }

  updateClicked() {
    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((currentProfile) => {
          const profile = {
            ...currentProfile,
            firstname: this.firstname.value,
            lastname: this.lastname.value,
            email: this.email.value,
            pictureUrl: this.pictureUrl.value,
          };

          this.store.dispatch(updateProfile({ profile }));
        })
      )
      .subscribe();
  }
}
