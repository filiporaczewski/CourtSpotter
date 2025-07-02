import { Injectable } from '@angular/core';
import {
  CourtAvailabilityGridClub,
  CourtAvailabilityGridColumn,
  CourtAvailabilityGridData,
  CourtAvailabilityGridItem
} from '../models/CourtAvailabilityGridModels';
import {GetCourtAvailabilitiesResponse} from './rest-api/court-availabilities/get-court-availabilities.response';
import {PadelCourtAvailabilityDto} from './rest-api/court-availabilities/padel-court-availability.dto';

@Injectable({
  providedIn: 'root'
})
export class CourtAvailabilityDataTransformationService {
  transformToGridData = (apiResponse: GetCourtAvailabilitiesResponse): CourtAvailabilityGridData => {
    // const gridItems: CourtAvailabilityGridItem[] = apiResponse.courtAvailabilities.map((dto: PadelCourtAvailabilityDto) => {
    //   return {
    //     courtName: dto.courtName,
    //     clubName: dto.clubName,
    //     startTime: new Date(dto.dateTime),
    //     price: dto.price,
    //     currency: dto.currency,
    //     bookingUrl: dto.bookingUrl,
    //     provider: dto.provider,
    //     durationInMinutes: dto.durationInMinutes,
    //     columnSpan: this.calculateColumnSpan(dto.durationInMinutes),
    //     courtType: dto.courtType
    //   }
    // });

    const mergedAvailabilities = this.mergeAvailabilitiesByCourtAndTime(apiResponse.courtAvailabilities);

    const timeSlotColumns: CourtAvailabilityGridColumn[] = [];

    // Generate columns for each half-hour slot from 6:00 to 22:30
    for(let hour = 6; hour < 23; hour++) {
      // Full hour (e.g., 6:00, 7:00, etc.)
      const fullHourItems = mergedAvailabilities.filter(item =>
        item.startTime.getHours() === hour && item.startTime.getMinutes() === 0
      );
      const fullHourClubGroups = this.groupByClub(fullHourItems);

      timeSlotColumns.push({
        startHour: hour,
        isHalfHour: false,
        gridClubs: fullHourClubGroups
      });

      // Half hour (e.g., 6:30, 7:30, etc.)
      const halfHourItems = mergedAvailabilities.filter(item =>
        item.startTime.getHours() === hour && item.startTime.getMinutes() === 30
      );
      const halfHourClubGroups = this.groupByClub(halfHourItems);

      timeSlotColumns.push({
        startHour: hour,
        isHalfHour: true,
        gridClubs: halfHourClubGroups
      });
    }

    return {
      items: timeSlotColumns,
      noData: timeSlotColumns.every(x => x.gridClubs.every(y => y.availabilities.length === 0))
    };
  }

  private mergeAvailabilitiesByCourtAndTime = (dtos: PadelCourtAvailabilityDto[]): CourtAvailabilityGridItem[] => {
    // Group by unique combination of court name, club name, start time, and provider
    const grouped = dtos.reduce((acc, dto) => {
      const key = `${dto.clubName}-${dto.courtName}-${dto.dateTime}-${dto.provider}`;

      if (!acc[key]) {
        acc[key] = {
          courtName: dto.courtName,
          clubName: dto.clubName,
          startTime: new Date(dto.dateTime),
          bookingUrl: dto.bookingUrl,
          provider: dto.provider,
          durationsInMinutes: [],
          courtType: dto.courtType
        };
      }

      // Add duration if not already present
      if (!acc[key].durationsInMinutes.includes(dto.durationInMinutes)) {
        acc[key].durationsInMinutes.push(dto.durationInMinutes);
      }

      return acc;
    }, {} as Record<string, CourtAvailabilityGridItem>);

    // Convert to array and sort durations
    return Object.values(grouped).map(item => ({
      ...item,
      durationsInMinutes: item.durationsInMinutes.sort((a, b) => a - b) // Sort durations ascending
    }));
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
}
