export interface GetUserResponse {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly isAdministrator: boolean;
  readonly email?: string;
  readonly avatarUrl?: string;
}
