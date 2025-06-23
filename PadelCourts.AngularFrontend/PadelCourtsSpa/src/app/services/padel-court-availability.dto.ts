export interface PadelCourtAvailabilityDto {
  id: string;
  clubName: string;
  courtName: string;
  dateTime: string;
  price: number;
  currency: string;
  bookingUrl: string;
  provider: string;
  durationInMinutes: number;
}
