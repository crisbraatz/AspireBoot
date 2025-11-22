import { HttpClient, HttpParams } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { Observable } from "rxjs";
import { environment } from "../../../environments/environment";
import { BaseListResponse } from "../models/base-list-response.model";
import { ListUserResponse } from "../models/users/list-user-response.model";

@Injectable({ providedIn: 'root' })
export class UsersService{
  private http = inject(HttpClient);

  listBy(email?: string): Observable<BaseListResponse<ListUserResponse>> {
    let params = new HttpParams();
    if (email) {
        params = params.set('email', email);
    }

    return this.http.get<BaseListResponse<ListUserResponse>>(
      `${environment.baseUrl}${environment.endpoints.users.listBy}`,
      { params, withCredentials: true }
    );
  }

}
