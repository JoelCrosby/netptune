import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { Maybe } from '../../modules/nothing';

@Injectable({
  providedIn: 'root'
})
export class AlertService {

  private _success = new Subject<string>();
  private _error = new Subject<string>();

  staticAlertClosed = false;
  estaticAlertClosed = false;

  successMessage: Maybe<string>;
  errorMessage: Maybe<string>;

  constructor() {

    setTimeout(() => this.staticAlertClosed = true, 6000);
    setTimeout(() => this.estaticAlertClosed = true, 6000);

    this._success.subscribe((message) => this.successMessage = message);
    this._success.pipe(
      debounceTime(5000)
    ).subscribe(() => this.successMessage = null);

    this._error.subscribe((message) => this.errorMessage = message);
    this._error.pipe(
      debounceTime(5000)
    ).subscribe(() => this.errorMessage = null);
  }

  public changeSuccessMessage(message: string) {
    this._success.next(message);
  }

  public changeErrorMessage(message: string) {
    this._error.next(message);
  }
}
