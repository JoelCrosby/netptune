export interface User {
  userId: string;
  email: string;
  email_verified: boolean;
  name: string;
  username: string;
  given_name: string;
  family_name: string;
  picture: string;
  zoneinfo: string;
  [key: string]: any;
}
