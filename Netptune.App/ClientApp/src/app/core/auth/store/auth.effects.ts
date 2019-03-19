import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Action } from '@ngrx/store';
import { Actions } from '@ngrx/effects';
import { LocalStorageService } from '@app/core/local-storage/local-storage.service';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions<Action>,
    private localStorageService: LocalStorageService,
    private router: Router
  ) {}


}
