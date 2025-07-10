import { Injectable } from '@angular/core';
import {CourtAvailabilitiesSearchFilters} from '../models/court-availabilities-search-filters';
import {BehaviorSubject} from 'rxjs';
import {DurationFilters} from '../models/duration-filters';
import {Params} from '@angular/router';
import {CourtType} from '../models/court-type';

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

  static getSelectedDurations = (durationFilters: DurationFilters) => {
    const durations: number[] = [];

    if (durationFilters.duration60) durations.push(60);
    if (durationFilters.duration90) durations.push(90);
    if (durationFilters.duration120) durations.push(120);

    return durations;
  }

  static constructQueryParamsFromFilters = (filters: CourtAvailabilitiesSearchFilters): Params => {
    const queryParams: Params = {};
    queryParams['date'] = filters.date.toISOString().split('T')[0];

    if (filters.clubIds.size > 0) {
      queryParams['clubs'] = Array.from(filters.clubIds);
    }

    if (filters.courtType !== null) {
      queryParams['courtType'] = filters.courtType;
    }

    const durations = CourtAvailabilitiesSearchFilterService.getSelectedDurations(filters.duration);

    debugger;

    if (durations.length > 0) {
      queryParams['durations'] = durations;
    }

    return queryParams;
  }

  static updateFiltersFromQueryParams = (queryParams: Params, currentFilters: CourtAvailabilitiesSearchFilters): CourtAvailabilitiesSearchFilters => {
    const filters = {...currentFilters};

    if (queryParams['date']) {
      filters.date = new Date(queryParams['date']);
    }

    if (queryParams['clubs']) {
      const clubs = Array.isArray(queryParams['clubs']) ? queryParams['clubs'] : [queryParams['clubs']];
      filters.clubIds = new Set(clubs);
    }

    if (queryParams['courtType']) {
      const courtTypeValue = parseInt(queryParams['courtType'], 10);

      if (courtTypeValue === CourtType.Indoor || courtTypeValue === CourtType.Outdoor) {
        filters.courtType = courtTypeValue as CourtType;
      }
    }

    if (queryParams['durations']) {
      const durations = Array.isArray(queryParams['durations']) ? queryParams['durations'] : [queryParams['durations']];

      filters.duration = {
        duration60: durations.includes('60'),
        duration90: durations.includes('90'),
        duration120: durations.includes('120')
      };
    }

    return filters;
  }
}
