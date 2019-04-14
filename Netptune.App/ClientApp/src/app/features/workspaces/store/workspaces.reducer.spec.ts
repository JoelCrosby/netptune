import { workspacesReducer, initialState } from './workspaces.reducer';

describe('Workspaces Reducer', () => {
  describe('an unknown action', () => {
    it('should return the previous state', () => {
      const action = {} as any;

      const result = workspacesReducer(initialState, action);

      expect(result).toBe(initialState);
    });
  });
});
