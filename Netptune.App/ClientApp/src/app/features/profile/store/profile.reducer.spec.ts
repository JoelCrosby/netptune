import { profileReducer, initialState } from './profile.reducer';

describe('Profile Reducer', () => {
  describe('an unknown action', () => {
    it('should return the previous state', () => {
      const action = {} as any;

      const result = profileReducer(initialState, action);

      expect(result).toBe(initialState);
    });
  });
});
