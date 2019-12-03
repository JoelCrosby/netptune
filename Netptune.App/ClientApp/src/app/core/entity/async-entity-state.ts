import { EntityState } from '@ngrx/entity';

export interface AsyncEntityState<TEntity> extends EntityState<TEntity> {
  loading: boolean;
  loaded: boolean;
  loadingError?: any;
  loadingCreate: boolean;
  createError?: any;
}
