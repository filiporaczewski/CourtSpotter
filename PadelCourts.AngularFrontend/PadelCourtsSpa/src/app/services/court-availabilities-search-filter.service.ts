import { Injectable } from '@angular/core';
import {CourtAvailabilitiesSearchFilters} from '../models/court-availabilities-search-filters';
import {BehaviorSubject} from 'rxjs';
import {DurationFilters} from '../models/duration-filters';

@Injectable({
  providedIn: 'root'
})
export class CourtAvailabilitiesSearchFilterService {
  private readonly initialFilters: CourtAvailabilitiesSearchFilters = {
    duration: {
      duration60: false,
      duration90: false,
      duration120: false
    },
    clubIds: new Set<string>(),
    date: new Date(),
    courtType: null
  }

  private filterSubject = new BehaviorSubject<CourtAvailabilitiesSearchFilters>(this.initialFilters);

  filters$ = this.filterSubject.asObservable();

  updateFilters = (filters: CourtAvailabilitiesSearchFilters) => {
    this.filterSubject.next(filters);
  }

  getSelectedDurations = (durationFilters: DurationFilters) => {
    const durations: number[] = [];

    if (durationFilters.duration60) durations.push(60);
    if (durationFilters.duration90) durations.push(90);
    if (durationFilters.duration120) durations.push(120);

    return durations;
  }
}
