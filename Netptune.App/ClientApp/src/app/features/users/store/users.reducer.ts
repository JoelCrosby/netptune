
import { UsersActions, UsersActionTypes } from './users.actions';

export interface State {

}

export const initialState: State = {

};

export function reducer(state = initialState, action: UsersActions): State {
  switch (action.type) {

    case UsersActionTypes.LoadUserss:
      return state;

    default:
      return state;
  }
}
