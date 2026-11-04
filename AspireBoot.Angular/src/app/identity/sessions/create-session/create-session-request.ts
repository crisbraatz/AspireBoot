export interface CreateSessionRequest {
  readonly email: string;
  readonly password: string;
  readonly rememberMe: boolean;
}
