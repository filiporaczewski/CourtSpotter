import { Injectable } from '@angular/core';
import {
  CourtAvailabilityGridClub,
  CourtAvailabilityGridColumn,
  CourtAvailabilityGridData,
  CourtAvailabilityGridItem
} from '../models/CourtAvailabilityGridModels';
import {GetCourtAvailabilitiesResponse} from './get-court-availabilities.response';
import {PadelCourtAvailabilityDto} from './padel-court-availability.dto';

@Injectable({
  providedIn: 'root'
})
export class CourtAvailabilityDataTransformationService {
  transformToGridData = (apiResponse: GetCourtAvailabilitiesResponse): CourtAvailabilityGridData => {
    const gridItems: CourtAvailabilityGridItem[] = apiResponse.courtAvailabilities.map((dto: PadelCourtAvailabilityDto) => {
      return {
        courtName: dto.courtName,
        clubName: dto.clubName,
        startTime: new Date(dto.dateTime),
        price: dto.price,
        currency: dto.currency,
        bookingUrl: dto.bookingUrl,
        provider: dto.provider,
        durationInMinutes: dto.durationInMinutes,
        columnSpan: this.calculateColumnSpan(dto.durationInMinutes)
      }
    });

    const hourColumns: CourtAvailabilityGridColumn[] = [];

    for(let hour = 6; hour < 23; hour++) {
      const hourItems = gridItems.filter(item => item.startTime.getHours() === hour);
      const clubGroups = this.groupByClub(hourItems);

      hourColumns.push({
        startHour: hour,
        gridClubs: clubGroups
      });
    }

    return {
      items: hourColumns
    };
  }

  private groupByClub = (items: CourtAvailabilityGridItem[]): CourtAvailabilityGridClub[] => {
    const grouped = items.reduce((acc, item) => {
      const clubName = this.extractClubName(item.clubName);

      if (!acc[clubName]) {
        acc[clubName] = [];
      }

      acc[clubName].push(item);
      return acc;
    }, {} as Record<string, CourtAvailabilityGridItem[]>);

    return Object.entries(grouped).map(([clubName, availabilities]) => ({
      clubName,
      availabilities: availabilities.sort((a, b) => a.startTime.getTime() - b.startTime.getTime())
    }));
  }

  private extractClubName = (clubName: string) => clubName || 'Unknown club'

  private calculateColumnSpan = (durationInMinutes: number) => durationInMinutes / 60
}
