import {DurationFilters} from './duration-filters';
import {CourtType} from './court-type';

export interface CourtAvailabilitiesSearchFilters {
  duration: DurationFilters,
  clubIds: Set<string>,
  date: Date,
  courtType: CourtType | null
}
