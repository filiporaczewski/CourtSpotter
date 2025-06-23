import {PadelCourtAvailabilityDto} from './padel-court-availability.dto';

export interface GetCourtAvailabilitiesResponse {
  totalCount: number;
  courtAvailabilities: PadelCourtAvailabilityDto[];
}
