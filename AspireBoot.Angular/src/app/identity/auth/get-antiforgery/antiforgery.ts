import { HttpBackend, HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { AppConfiguration } from '../../../shared/configuration/app-configuration';
import { GetAntiforgeryResponse } from './get-antiforgery-response';

@Injectable({ providedIn: 'root' })
export class Antiforgery {
  private readonly http = new HttpClient(inject(HttpBackend));
  private readonly configuration = inject(AppConfiguration);

  async getToken(): Promise<string> {
    // Request a token for the current principal; never reuse an anonymous token after login.
    return (
      await firstValueFrom(
        this.http.get<GetAntiforgeryResponse>(this.configuration.value.apiUrl + '/auth/csrf', {
          withCredentials: true,
        }),
      )
    ).token;
  }
}
