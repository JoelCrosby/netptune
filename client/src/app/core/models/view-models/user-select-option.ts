export interface UserSelectOption {
  id: string;
  displayName: string;
  email?: string | null;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}

export interface UserSelectValue {
  id: string;
  displayName: string;
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}
