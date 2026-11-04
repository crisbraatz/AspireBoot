export interface GetCurrentSessionResponse {
  readonly identityUserId: string;
  readonly userId: string;
  readonly email: string;
  readonly isAdministrator: boolean;
}
