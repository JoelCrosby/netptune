import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  OnInit,
} from '@angular/core';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
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
  formGroup = new FormGroup({
    firstname: new FormControl(),
    lastname: new FormControl(),
    email: new FormControl(),
    pictureUrl: new FormControl(),
  });

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
      select(ProfileSelectors.selectUpdateProfileLoading),
      tap((loading) =>
        loading ? this.formGroup.disable() : this.formGroup.enable()
      ),
      shareReplay()
    );

    this.formGroup = this.fb.group({
      firstname: {
        updateOn: 'blur',
        validators: [Validators.required],
      },
      lastname: {
        updateOn: 'blur',
        validators: [Validators.required],
      },
      email: {
        updateOn: 'blur',
        validators: [Validators.required],
      },
      pictureUrl: {
        updateOn: 'blur',
        validators: [Validators.maxLength(1024)],
      },
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
