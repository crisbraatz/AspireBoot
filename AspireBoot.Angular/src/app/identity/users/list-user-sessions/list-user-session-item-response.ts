export interface ListUserSessionItemResponse {
  readonly id: string;
  readonly createdAtUtc: string;
  readonly expiresAtUtc: string;
  readonly browser: string;
  readonly operatingSystem: string;
  readonly device: string;
  readonly isCurrent?: boolean;
  readonly remoteIpAddress?: string;
}
