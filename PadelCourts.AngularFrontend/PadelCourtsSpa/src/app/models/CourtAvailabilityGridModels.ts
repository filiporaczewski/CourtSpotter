import {CourtType} from './court-type';

export interface CourtAvailabilityGridItem {
  courtName: string;
  clubName: string;
  startTime: Date;
  // price: number;
  // currency: string;
  bookingUrl: string;
  provider: string;
  durationsInMinutes: number[];
  // columnSpan: number;
  courtType: CourtType
}

export interface CourtAvailabilityGridClub {
  clubName: string,
  availabilities: CourtAvailabilityGridItem[]
}

export interface CourtAvailabilityGridColumn {
  startHour: number,
  gridClubs: CourtAvailabilityGridClub[]
  isHalfHour: boolean
}

export interface CourtAvailabilityGridData {
  noData: boolean,
  items: CourtAvailabilityGridColumn[]
}
