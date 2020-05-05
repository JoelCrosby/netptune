export interface ActionState {
  loading: boolean;
  error?: any;
}

export const DefaultActionState: ActionState = {
  loading: false,
};
