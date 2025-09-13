import { AfterViewInit, Component, OnInit, ChangeDetectionStrategy, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { confirmEmail } from '@core/auth/store/auth.actions';
import { AuthCodeRequest } from '@core/auth/store/auth.models';
import { selectIsConfirmEmailLoading } from '@core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, tap } from 'rxjs/operators';
import { AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
    templateUrl: './confirm-view.component.html',
    styleUrls: ['./confirm-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatProgressSpinner, AsyncPipe]
})
export class ConfirmViewComponent implements OnInit, AfterViewInit {
  private activatedRoute = inject(ActivatedRoute);
  private store = inject(Store);

  loading$!: Observable<boolean>;

  private request?: AuthCodeRequest;

  constructor() {
    this.activatedRoute.data
      .pipe(
        first(),
        tap((data) => {
          this.request = data.confirmEmail as AuthCodeRequest;
        })
      )
      .subscribe();
  }

  ngOnInit() {
    this.loading$ = this.store.select(selectIsConfirmEmailLoading);
  }

  ngAfterViewInit() {
    if (!this.request) return;

    this.store.dispatch(confirmEmail({ request: this.request }));
  }
}
