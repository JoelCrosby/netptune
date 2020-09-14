export interface ChangePasswordRequest {
  userId: string;
  currentPassword: string;
  newPassword: string;
}
