import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { FormBuilder, Validators } from '@angular/forms';
import { AppUser } from '@core/models/appuser';
import { select, Store } from '@ngrx/store';
import {
  updateProfile,
  uploadProfilePicture,
} from '@profile/store/profile.actions';
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
  formGroup = this.fb.group({
    firstname: ['', [Validators.required]],
    lastname: ['', [Validators.required]],
    email: ['', [Validators.required]],
    pictureUrl: [''],
  });

  onDestroy$ = new Subject<void>();
  loadingUpdate$!: Observable<boolean>;
  editProfilePicture$ = new Subject<boolean>();

  data?: FormData;

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

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          const value = profile as AppUser;

          this.formGroup.setValue(
            {
              firstname: value.firstname,
              lastname: value.lastname,
              email: value.email,
              pictureUrl: value.pictureUrl ?? null,
            },
            { emitEvent: false }
          );

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
            firstname: this.formGroup.controls.firstname.value as string,
            lastname: this.formGroup.controls.lastname.value as string,
            email: this.formGroup.controls.email.value as string,
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
    this.formGroup.controls.pictureUrl.setValue(src);

    this.store
      .select(ProfileSelectors.selectProfile)
      .pipe(
        filter((profile) => !!profile),
        first(),
        tap((profile) => {
          if (!profile) return;

          this.store.dispatch(updateProfile({ profile }));
          this.store.dispatch(uploadProfilePicture({ data }));
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

    this.formGroup.controls.pictureUrl.reset();
    this.editProfilePicture$.next(false);
  }
}
