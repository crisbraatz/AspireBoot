import { BaseResponse } from "./base.response.model";

export interface AuthTokenData {
  authToken: string;
}

export type RefreshTokenResponse = BaseResponse<AuthTokenData>;
