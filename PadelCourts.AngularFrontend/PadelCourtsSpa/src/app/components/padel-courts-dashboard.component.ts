import {Component, inject, OnInit} from '@angular/core';
import {CourtAvailabilityGridData} from '../models/CourtAvailabilityGridModels';
import {map, Observable} from 'rxjs';
import {PadelCourtAvailabilitiesApiService} from '../services/rest-api/court-availabilities/padel-court-availabilities.api.service';
import {CourtAvailabilityDataTransformationService} from '../services/court-availability-data-transformation.service';
import {AsyncPipe, DatePipe} from '@angular/common';
import {CourtAvailabilityGridColumnComponent} from './court-availability-grid-column.component';
import {BookingTimePipe} from '../pipes/booking-time.pipe';
import {FormsModule} from '@angular/forms';
import {PadelCourtsSearchFiltersComponent} from './padel-courts-search-filters.component';
import {CourtAvailabilitiesSearchFilters} from '../models/court-availabilities-search-filters';
import {CourtAvailabilitiesSearchFilterService} from '../services/court-availabilities-search-filter.service';
import {PadelClub} from '../models/padel-club';
import {PadelClubsApiService} from '../services/rest-api/padel-clubs/padel-clubs.api.service';

@Component({
  selector: 'app-padel-courts-dashboard',
  imports: [
    AsyncPipe,
    CourtAvailabilityGridColumnComponent,
    BookingTimePipe,
    FormsModule,
    PadelCourtsSearchFiltersComponent,
    DatePipe
  ],
  template: `
    @if (data$ | async; as data) {
      <section class="">
        <section class="flex flex-col items-center">
          <h2 class="font-mono text-4xl font-bold px-8 mt-8 mb-4">Padel courts search</h2>
          @if(filters$ | async; as filters) {
            @if(padelClubs$ | async; as padelClubs) {
              <app-padel-courts-search-filters [maxDaysAhead]="14" [filters]="filters" [padelClubs]="padelClubs" (filtersApplied)="applyFilters($event)" />
            }
            @if(!data.noData) {
              <h3 class="font-bold font-mono px-8 mb-6 text-2xl text-white">Courts available on {{filters.date | date}}</h3>
            } @else {
              <h3 class="font-bold font-mono px-8 mb-6 text-2xl text-white">No results</h3>
            }
          }
        </section>
        @if(!data.noData) {
          <div class="w-fit max-w-[80%] mx-auto overflow-x-auto border border-gray-300 rounded-lg">
            <div class="flex min-w-max">
              @for(item of data.items; track $index) {
                @if(item.gridClubs.length !== 0) {
                  <div class="flex-1 w-[250px] min-w-fit border-r border-gray-300 last:border-r-0 bg-slate-900">
                    <!-- Header -->
                    <div class="bg-slate-700 p-4 border-b border-gray-300 text-center text-white font-bold sticky top-0 z-10">
                      <div>{{ item | bookingTime }}</div>
                    </div>
                    <!-- Content -->
                    <div class="p-4 min-h-[200px]">
                      <app-court-availability-grid-column [column]="item" />
                    </div>
                  </div>
                }
              }
            </div>
          </div>
        }
      </section>
    }
  `,
  styles: [
    `
    `
  ]
})
export class PadelCourtsDashboardComponent implements OnInit {
  private courtAvailabilitiesApiService = inject(PadelCourtAvailabilitiesApiService);
  private padelClubsApiService = inject(PadelClubsApiService);
  private gridDataTransformService = inject(CourtAvailabilityDataTransformationService);
  private filtersService = inject(CourtAvailabilitiesSearchFilterService);

  data$?: Observable<CourtAvailabilityGridData>;
  filters$?: Observable<CourtAvailabilitiesSearchFilters>;
  padelClubs$?: Observable<PadelClub[]>;

  applyFilters(filters: CourtAvailabilitiesSearchFilters): void {
    this.filtersService.updateFilters(filters);
    this.loadData(filters);
  }

  private loadData(filters?: CourtAvailabilitiesSearchFilters): void {
    const selectedDurations = (filters && filters.duration) ? this.filtersService.getSelectedDurations(filters.duration) : undefined;
    const selectedClubIds = (filters && filters.clubIds) ? Array.from(filters.clubIds) : undefined;
    const date = (filters && filters.date) ? filters.date : new Date();
    const courtType = (filters && filters.courtType !== null) ? filters.courtType : undefined

    this.data$ = this.courtAvailabilitiesApiService.getAvailabilities(date, selectedDurations, selectedClubIds, courtType)
      .pipe(map(response => this.gridDataTransformService.transformToGridData(response)));
  }

  ngOnInit(): void {
    this.filters$ = this.filtersService.filters$;
    this.padelClubs$ = this.padelClubsApiService.getClubs().pipe(map(response => {
      return response.clubs.map(dto => ({
        name: dto.name,
        id: dto.clubId
      }))
    }));

    this.loadData();
  }
}
