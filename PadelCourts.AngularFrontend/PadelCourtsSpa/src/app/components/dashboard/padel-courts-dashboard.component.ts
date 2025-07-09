import {Component, inject, OnInit} from '@angular/core';
import {CourtAvailabilityGridData} from '../../models/CourtAvailabilityGridModels';
import {map, Observable} from 'rxjs';
import {PadelCourtAvailabilitiesApiService} from '../../services/rest-api/court-availabilities/padel-court-availabilities.api.service';
import {CourtAvailabilityDataTransformationService} from '../../services/court-availability-data-transformation.service';
import {AsyncPipe, DatePipe} from '@angular/common';
import {CourtAvailabilityGridColumnComponent} from './court-availability-grid-column.component';
import {BookingTimePipe} from '../../pipes/booking-time.pipe';
import {FormsModule} from '@angular/forms';
import {PadelCourtsSearchFiltersComponent} from './search-filters/padel-courts-search-filters.component';
import {CourtAvailabilitiesSearchFilters} from '../../models/court-availabilities-search-filters';
import {CourtAvailabilitiesSearchFilterService} from '../../services/court-availabilities-search-filter.service';
import {PadelClub} from '../../models/padel-club';
import {PadelClubsApiService} from '../../services/rest-api/padel-clubs/padel-clubs.api.service';
import {ThemePickerComponent} from '../theme-picker/theme-picker.component';
import {TranslocoPipe} from '@jsverse/transloco';
import {LanguageSwitcherComponent} from '../language-switcher/language-switcher.component';
import {TranslocoDatePipe} from '@jsverse/transloco-locale';

@Component({
  selector: 'app-padel-courts-dashboard',
  imports: [
    AsyncPipe,
    CourtAvailabilityGridColumnComponent,
    BookingTimePipe,
    FormsModule,
    PadelCourtsSearchFiltersComponent,
    DatePipe,
    ThemePickerComponent,
    TranslocoPipe,
    LanguageSwitcherComponent,
    TranslocoDatePipe
  ],
  template: `
    @if (data$ | async; as data) {
      <section class="mb-16">
        <section class="flex flex-col items-center justify-center">
          <div class="flex flex-col items-center mt-8 md:mb-2 md:flex-row gap-3">
            <h2 class="text-center font-mono text-xl md:text-4xl px-4 font-bold text-gray-900 dark:text-white mb-1 md:mb-0">{{ 'title' | transloco }}</h2>
            <app-language-switcher class="md:block hidden" />
            <app-theme-picker class="md:block hidden" />
          </div>
          <div class="md:hidden flex gap-2 mt-4">
            <app-language-switcher />
            <app-theme-picker />
          </div>
          @if(filters$ | async; as filters) {
            @if(padelClubs$ | async; as padelClubs) {
              <app-padel-courts-search-filters [maxDaysAhead]="14" [filters]="filters" [padelClubs]="padelClubs" (filtersApplied)="applyFilters($event)" />
            }
            @if(!data.noData) {
              <h3 class="font-bold font-mono px-8 mb-6 text-xl md:text-2xl text-gray-900 dark:text-white">{{ 'grid.title' | transloco: { date: filters.date | translocoDate} }}</h3>
            } @else {
              <h3 class="font-bold font-mono px-8 mb-6 text-l md:text-2xl text-gray-900 dark:text-white">{{ 'grid.no_results' | transloco }}</h3>
            }
          }
        </section>
        @if(!data.noData) {
          <div class="w-fit max-w-[95%] lg:max-w-[90%] xl:max-w-[70%] mx-auto overflow-x-auto border border-gray-300 dark:border-gray-600 rounded-lg max-h-[800px] overflow-y-auto">
            <div class="flex min-w-max">
              @for(item of data.items; track $index) {
                @if(item.gridClubs.length !== 0) {
                  <div class="flex-1 w-[250px] min-w-fit border-r border-gray-300 dark:border-gray-600 last:border-r-0 bg-gray-100 dark:bg-slate-900">
                    <!-- Header -->
                    <div class="bg-white dark:bg-slate-800 p-4 border-b border-gray-300 dark:border-gray-600 text-center text-gray-900 dark:text-white font-bold sticky top-0 z-10">
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
