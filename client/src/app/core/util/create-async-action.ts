import { HttpErrorResponse } from '@angular/common/http';
import {
  Action,
  ActionCreator,
  ActionCreatorProps,
  NotAllowedCheck,
  createAction,
  props,
} from '@ngrx/store';

const withCheck = <P extends object>(
  propsCreator: ActionCreatorProps<P>
): ActionCreatorProps<P> & NotAllowedCheck<P> =>
  propsCreator as ActionCreatorProps<P> & NotAllowedCheck<P>;

type AsyncActionCreator<Type extends string, Payload extends object> = [
  Payload,
] extends [never]
  ? ActionCreator<Type, () => Action<Type>>
  : ActionCreator<Type, (props: Payload) => Payload & Action<Type>>;

export interface AsyncAction<
  Type extends string,
  Init extends object,
  Success extends object,
  Fail extends object,
> {
  init: AsyncActionCreator<Type, Init>;
  success: AsyncActionCreator<`${Type} Success`, Success>;
  fail: AsyncActionCreator<`${Type} Fail`, Fail>;
}

export interface AsyncActionProps<
  Init extends object,
  Success extends object,
  Fail extends object,
> {
  init?: ActionCreatorProps<Init>;
  success?: ActionCreatorProps<Success>;
  fail?: ActionCreatorProps<Fail>;
}

export function createAsyncAction<
  Type extends string,
  Init extends object = never,
  Success extends object = never,
  Fail extends object = { error: HttpErrorResponse },
>(
  type: Type,
  config?: AsyncActionProps<Init, Success, Fail>
): AsyncAction<Type, Init, Success, Fail> {
  const failProps =
    config?.fail ??
    (props<{ error: HttpErrorResponse }>() as ActionCreatorProps<Fail>);

  return {
    init: (config?.init
      ? createAction(type, withCheck(config.init))
      : createAction(type)) as AsyncActionCreator<Type, Init>,
    success: (config?.success
      ? createAction(`${type} Success`, withCheck(config.success))
      : createAction(`${type} Success`)) as AsyncActionCreator<
      `${Type} Success`,
      Success
    >,
    fail: createAction(
      `${type} Fail`,
      withCheck(failProps)
    ) as AsyncActionCreator<`${Type} Fail`, Fail>,
  };
}
