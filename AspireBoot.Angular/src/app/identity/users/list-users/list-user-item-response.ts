export interface ListUserItemResponse {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
  readonly isAdministrator: boolean;
  readonly avatarUrl?: string;
}
