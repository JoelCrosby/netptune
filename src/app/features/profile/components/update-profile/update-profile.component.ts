import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { select, Store } from '@ngrx/store';
import { loadProfile, updateProfile } from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable, Subject } from 'rxjs';
import { filter, first, shareReplay, takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-update-profile',
  templateUrl: './update-profile.component.html',
  styleUrls: ['./update-profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdateProfileComponent
  implements OnInit, OnDestroy, AfterViewInit {
  formGroup: FormGroup;
  onDestroy$ = new Subject();
  loadingUpdate$: Observable<boolean>;

  get firstname() {
    return this.formGroup.get('firstname');
  }
  get lastname() {
    return this.formGroup.get('lastname');
  }
  get email() {
    return this.formGroup.get('email');
  }
  get pictureUrl() {
    return this.formGroup.get('pictureUrl');
  }

  constructor(private store: Store, private fb: FormBuilder) {}

  ngOnInit() {
    this.loadingUpdate$ = this.store.pipe(
      takeUntil(this.onDestroy$),
      select(ProfileSelectors.selectUpdateProfileLoading),
      tap((loading) =>
        loading ? this.formGroup.disable() : this.formGroup.enable()
      ),
      shareReplay()
    );

    this.formGroup = this.fb.group({
      firstname: ['', Validators.required],
      lastname: ['', Validators.required],
      email: ['', Validators.required],
      pictureUrl: ['', Validators.maxLength(1024)],
    });

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          this.firstname.setValue(profile.firstname, { emitEvent: false });
          this.lastname.setValue(profile.lastname, { emitEvent: false });
          this.email.setValue(profile.email, { emitEvent: false });
          this.pictureUrl.setValue(profile.pictureUrl, { emitEvent: false });
        })
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProfile());
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
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
