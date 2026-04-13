import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { SessionsService } from "./sessions.service";
import { BaseListResponse } from "../models/base-list-response.model";
import { RefreshSessionResponse } from "../models/sessions/refresh-session-response.model";
import { CreateUserRequest } from "../models/users/create-user-request.model";
import { ListUserResponse } from "../models/users/list-user-response.model";
import { ListUsersRequest } from "../models/users/list-users-request.model";
import { environment } from "../../../environments/environment";

@Injectable({ providedIn: 'root' })
export class UsersService {
  private http = inject(HttpClient);
  private sessionsService = inject(SessionsService);

  list(data: ListUsersRequest): Observable<BaseListResponse<ListUserResponse>> {
    let params = new HttpParams();

    if (data.email)
      params = params.set('email', data.email);

    return this.http.get<BaseListResponse<ListUserResponse>>(
      `${environment.baseUrl}${environment.endpoints.users.list}`,
      { params, withCredentials: true }
    );
  }

  signUp(data: CreateUserRequest): Observable<RefreshSessionResponse> {
    this.sessionsService.clearTokens();

    return this.http.post<RefreshSessionResponse>(
      `${environment.baseUrl}${environment.endpoints.users.create}`,
      data,
      { withCredentials: true }
    );
  }

}
