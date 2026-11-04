export interface ConfirmEmailChangeRequest {
  readonly email: string;
  readonly identityUserId: string;
  readonly token: string;
}
