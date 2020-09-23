import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { AppUser } from '@core/models/appuser';
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
  editProfilePicture$ = new Subject<boolean>();

  data?: FormData;

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

  constructor(
    private store: Store,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef
  ) {}

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
      pictureUrl: [''],
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

          this.cd.markForCheck();
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
          };

          this.store.dispatch(updateProfile({ profile }));
        })
      )
      .subscribe();
  }

  onChangePictureClicked() {
    this.editProfilePicture$.next(true);
  }

  onCropped({ blob, src }: { blob: Blob; src: string }) {
    if (!blob) return;

    const data = new FormData();

    data.append('file', blob, 'profile-picture');

    this.editProfilePicture$.next(false);
    this.pictureUrl.setValue(src);

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          this.store.dispatch(updateProfile({ profile, data }));
        })
      )
      .subscribe();
  }

  onCropperCanceled() {
    this.editProfilePicture$.next(false);
  }

  onCropperCleared() {
    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((current) => {
          const profile: AppUser = {
            ...current,
            pictureUrl: null,
          };
          this.store.dispatch(updateProfile({ profile }));
        })
      )
      .subscribe();

    this.pictureUrl.reset();
    this.editProfilePicture$.next(false);
  }
}
