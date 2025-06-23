import {inject, Injectable} from '@angular/core';
import {HttpClient, HttpParams} from '@angular/common/http';
import {GetCourtAvailabilitiesResponse} from './get-court-availabilities.response';
import {Observable} from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PadelCourtAvailabilitiesApiService {
  private readonly baseUrl: string = 'https://localhost:7043/api'

  private http = inject(HttpClient);

  getAvailabilities(date: Date): Observable<GetCourtAvailabilitiesResponse> {
    const startDate = new Date(date);
    startDate.setHours(0, 0, 0, 0);

    const endDate = new Date(date);
    endDate.setHours(23, 59, 59, 999);

    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<GetCourtAvailabilitiesResponse>(
      `${this.baseUrl}/court-availabilities`,
      { params }
    );
  }
}
