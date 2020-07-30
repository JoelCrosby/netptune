import { AfterViewInit, Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { confirmEmail } from '@app/core/auth/store/auth.actions';
import { ConfirmEmailRequest } from '@app/core/auth/store/auth.models';
import { selectIsConfirmEmailLoading } from '@app/core/auth/store/auth.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { first, tap } from 'rxjs/operators';

@Component({
  templateUrl: './confirm-view.component.html',
  styleUrls: ['./confirm-view.component.scss'],
})
export class ConfirmViewComponent implements OnInit, AfterViewInit {
  private request: ConfirmEmailRequest;

  loading$: Observable<boolean>;

  constructor(private activatedRoute: ActivatedRoute, private store: Store) {
    this.activatedRoute.data
      .pipe(
        first(),
        tap((data) => {
          this.request = data.confirmEmail as ConfirmEmailRequest;
          console.log({ request: this.request });
        })
      )
      .subscribe();
  }

  ngOnInit() {
    this.loading$ = this.store.select(selectIsConfirmEmailLoading);
  }

  ngAfterViewInit() {
    this.store.dispatch(confirmEmail({ request: this.request }));
  }
}
