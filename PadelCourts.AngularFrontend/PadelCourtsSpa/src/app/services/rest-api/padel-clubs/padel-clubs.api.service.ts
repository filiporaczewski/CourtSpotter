import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {PadelClubsResponse} from './padel-clubs.response';

@Injectable({
  providedIn: 'root'
})
export class PadelClubsApiService {
  private readonly baseUrl: string = 'https://localhost:7043/api'
  private http = inject(HttpClient);

  getClubs = (): Observable<PadelClubsResponse> => {
    const endpointUrl = `${this.baseUrl}/padel-clubs`
    return this.http.get<PadelClubsResponse>(endpointUrl);
  }
}
