import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Observable} from 'rxjs';
import {PadelClubsResponse} from './padel-clubs.response';
import {environment} from '../../../../environments/environment';
import {PadelClubAdmin} from '../../../models/PadelClubAdmin';
import {PadelClubAddCommand} from '../../../models/padel-club-add-command';

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

  addClub = (command: PadelClubAddCommand): Observable<any> => {
    const endpointUrl = `${this.baseUrl}/padel-clubs`
    return this.http.post(endpointUrl, command);
  }
}
