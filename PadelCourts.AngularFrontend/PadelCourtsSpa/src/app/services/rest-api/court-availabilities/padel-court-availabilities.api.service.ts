import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {GetCourtAvailabilitiesResponse} from './get-court-availabilities.response';
import {Observable} from 'rxjs';
import {CourtType} from '../../../models/court-type';

@Injectable({
  providedIn: 'root'
})
export class PadelCourtAvailabilitiesApiService {
  private readonly baseUrl: string = 'https://localhost:7043/api'
  private http = inject(HttpClient);

  getAvailabilities(date: Date, durations?: number[], clubIds?: string[], courtType?: CourtType): Observable<GetCourtAvailabilitiesResponse> {
    const startDate = new Date(date);
    startDate.setUTCHours(0, 0, 0, 0);

    const endDate = new Date(date);
    endDate.setUTCHours(23, 59, 59, 999);

    let params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    if (durations && durations.length > 0) {
      durations.forEach(duration => {
        params = params.append('durations', duration.toString());
      });
    }

    if(clubIds && clubIds.length > 0) {
      clubIds.forEach(clubId => {
        params = params.append('clubIds', clubId);
      });
    }

    if (courtType !== undefined) {
      params = params.append('courtType', courtType);
    }

    return this.http.get<GetCourtAvailabilitiesResponse>(
      `${this.baseUrl}/court-availabilities`,
      { params }
    );
  }
}
