export interface ConfirmEmailRequest {
  readonly identityUserId: string;
  readonly token: string;
}
