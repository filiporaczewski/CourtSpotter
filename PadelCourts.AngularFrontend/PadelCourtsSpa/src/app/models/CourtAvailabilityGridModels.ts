export interface CourtAvailabilityGridItem {
  courtName: string;
  clubName: string;
  startTime: Date;
  price: number;
  currency: string;
  bookingUrl: string;
  provider: string;
  durationInMinutes: number;
  columnSpan: number;
}

export interface CourtAvailabilityGridClub {
  clubName: string,
  availabilities: CourtAvailabilityGridItem[]
}

export interface CourtAvailabilityGridColumn {
  startHour: number,
  gridClubs: CourtAvailabilityGridClub[]
}

export interface CourtAvailabilityGridData {
  items: CourtAvailabilityGridColumn[]
}
