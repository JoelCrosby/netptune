import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { AppState } from '@core/core.state';
import { AppUser } from '@core/models/appuser';
import { select, Store } from '@ngrx/store';
import { updateProfile } from '@profile/store/profile.actions';
import * as ProfileSelectors from '@profile/store/profile.selectors';
import { Observable, Subject } from 'rxjs';
import { filter, first, shareReplay, takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-update-profile',
  templateUrl: './update-profile.component.html',
  styleUrls: ['./update-profile.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UpdateProfileComponent implements OnInit, OnDestroy {
  formGroup = new FormGroup({
    firstname: new FormControl('', [Validators.required]),
    lastname: new FormControl('', [Validators.required]),
    email: new FormControl('', [Validators.required]),
    pictureUrl: new FormControl(''),
  });

  onDestroy$ = new Subject<void>();
  loadingUpdate$!: Observable<boolean>;
  editProfilePicture$ = new Subject<boolean>();

  data?: FormData;

  get firstname() {
    return this.formGroup.controls.firstname;
  }
  get lastname() {
    return this.formGroup.controls.lastname;
  }
  get email() {
    return this.formGroup.controls.email;
  }
  get pictureUrl() {
    return this.formGroup.controls.pictureUrl;
  }
  get pictureUrlValue() {
    return this.pictureUrl.value as string;
  }

  constructor(private store: Store<AppState>, private cd: ChangeDetectorRef) {}

  ngOnInit() {
    this.loadingUpdate$ = this.store.pipe(
      takeUntil(this.onDestroy$),
      select(ProfileSelectors.selectUpdateProfileLoading),
      tap((loading) =>
        loading ? this.formGroup.disable() : this.formGroup.enable()
      ),
      shareReplay()
    );

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          const value = profile as AppUser;

          this.firstname.setValue(value.firstname, { emitEvent: false });
          this.lastname.setValue(value.lastname, { emitEvent: false });
          this.email.setValue(value.email, { emitEvent: false });
          this.pictureUrl.setValue(value.pictureUrl as string, {
            emitEvent: false,
          });

          this.cd.markForCheck();
        })
      )
      .subscribe();
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
          if (!currentProfile) {
            return;
          }

          const profile: AppUser = {
            ...currentProfile,
            firstname: this.firstname.value as string,
            lastname: this.lastname.value as string,
            email: this.email.value as string,
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

    data.append('file', blob, 'profile-picture.png');

    this.editProfilePicture$.next(false);
    this.pictureUrl.setValue(src);

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          if (!profile) return;

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
          if (!current) {
            return;
          }

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
