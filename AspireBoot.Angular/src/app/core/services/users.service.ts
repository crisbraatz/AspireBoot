import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { BaseListResponse } from "../models/base-list-response.model";
import { ListUsersRequest } from "../models/users/list-users-request.model";
import { ListUserResponse } from "../models/users/list-user-response.model";

@Injectable({ providedIn: 'root' })
export class UsersService {
  private http = inject(HttpClient);

  listBy(data: ListUsersRequest): Observable<BaseListResponse<ListUserResponse>> {
    let params = new HttpParams();
    if (data.email)
      params = params.set('email', data.email);

    return this.http.get<BaseListResponse<ListUserResponse>>(
      `${environment.baseUrl}${environment.endpoints.users.listBy}`,
      { params, withCredentials: true }
    );
  }

}
