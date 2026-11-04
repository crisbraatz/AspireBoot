export interface ResetPasswordRequest {
  readonly email: string;
  readonly password: string;
  readonly token: string;
}
