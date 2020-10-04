import { EntityState } from '@ngrx/entity';
import { HttpErrorResponse } from '@angular/common/http';

export interface AsyncEntityState<TEntity> extends EntityState<TEntity> {
  loading: boolean;
  loaded: boolean;
  loadingError?: HttpErrorResponse;
  loadingCreate: boolean;
  createError?: HttpErrorResponse;
}
