import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {PadelClubsResponse} from './padel-clubs.response';
import {environment} from '../../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PadelClubsApiService {
  private readonly baseUrl: string = environment.apiBaseUrl
  private http = inject(HttpClient);

  getClubs = (): Observable<PadelClubsResponse> => {
    const endpointUrl = `${this.baseUrl}/padel-clubs`
    return this.http.get<PadelClubsResponse>(endpointUrl);
  }
}
