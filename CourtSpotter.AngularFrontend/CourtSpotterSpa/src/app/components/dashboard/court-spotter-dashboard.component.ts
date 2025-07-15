import {Component, DestroyRef, inject, OnInit} from '@angular/core';
import {CourtAvailabilityGridData} from '../../models/CourtAvailabilityGridModels';
import {combineLatest, distinctUntilChanged, map, Observable} from 'rxjs';
import {PadelCourtAvailabilitiesApiService} from '../../services/rest-api/court-availabilities/padel-court-availabilities.api.service';
import {CourtAvailabilityDataTransformationService} from '../../services/court-availability-data-transformation.service';
import {AsyncPipe} from '@angular/common';
import {CourtAvailabilityGridColumnComponent} from './court-availability-grid-column.component';
import {BookingTimePipe} from '../../pipes/booking-time.pipe';
import {FormsModule} from '@angular/forms';
import {PadelCourtsSearchFiltersComponent} from './search-filters/padel-courts-search-filters.component';
import {CourtAvailabilitiesSearchFilters} from '../../models/court-availabilities-search-filters';
import {CourtAvailabilitiesSearchFilterService} from '../../services/court-availabilities-search-filter.service';
import {PadelClub} from '../../models/padel-club';
import {PadelClubsApiService} from '../../services/rest-api/padel-clubs/padel-clubs.api.service';
import {TranslocoPipe} from '@jsverse/transloco';
import {TranslocoDatePipe} from '@jsverse/transloco-locale';
import {ActivatedRoute, Router} from '@angular/router';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {CsLoaderComponent} from '../../shared/components/cs-loader/cs-loader.component';
import {AsyncState} from '../../models/AsyncState';
import {AsyncStateService} from '../../shared/services/async-state.service';
import {ErrorBoxComponent} from '../../shared/components/error-box/error-box.component';

@Component({
  selector: 'app-padel-courts-dashboard',
  imports: [
    AsyncPipe,
    CourtAvailabilityGridColumnComponent,
    BookingTimePipe,
    FormsModule,
    PadelCourtsSearchFiltersComponent,
    TranslocoPipe,
    TranslocoDatePipe,
    CsLoaderComponent,
    ErrorBoxComponent
  ],
  template: `
      <section class="mb-16">
        <section class="flex flex-col items-center justify-center">
          <div class="flex flex-col items-center mt-7 md:mt-14 mb-3 md:mb-6 md:flex-row gap-3">
            <h2 class="text-center font-mono text-3xl md:text-5xl px-4 font-bold text-gray-900 dark:text-white mb-1 md:mb-0">{{ 'title' | transloco }}</h2>
          </div>
          @if (filters$ | async; as filters) {
            @if (padelClubsState$ | async; as padelClubsState) {
              <app-padel-courts-search-filters class="w-full" [maxDaysAhead]="14" [filters]="filters" [padelClubsState]="padelClubsState" (filtersApplied)="applyFilters($event)"/>
            }
        }
        </section>
        @if(courtAvailabilityData$ | async ; as courtAvailabilitiesState) {
          @if(courtAvailabilitiesState.loading) {
            <app-cs-loader containerAdditionalClasses="py-40" [spinnerSize]="20" />
          }

          @if(courtAvailabilitiesState.error) {
            <div class="p-20">
              <app-error-box [displayIcon]="true" />
            </div>
          }

          @if(courtAvailabilitiesState.data ; as data) {
            @if(data.noData) {
              <h3 class="text-center font-bold font-mono px-8 p-30 text-l md:text-2xl text-gray-900 dark:text-white">{{ 'grid.no_results' | transloco }}</h3>
            } @else {
              <h3 class="text-center font-bold font-mono px-8 mb-6 text-xl md:text-2xl text-gray-900 dark:text-white">{{ 'grid.title' | transloco: {date: date | translocoDate} }}</h3>
              <div class="w-full mx-auto overflow-x-auto border border-gray-300 dark:border-gray-600 rounded-lg max-h-[800px] overflow-y-auto">
<!--              <div class="w-fit max-w-[95%] lg:max-w-[90%] xl:max-w-[70%] mx-auto overflow-x-auto border border-gray-300 dark:border-gray-600 rounded-lg max-h-[800px] overflow-y-auto">-->
                <div class="flex min-w-max">
                  @for (item of data.items; track $index) {
                    @if (item.gridClubs.length !== 0) {
                      <div
                        class="flex-1 w-[250px] min-w-fit border-r border-gray-300 dark:border-gray-600 last:border-r-0 bg-gray-100 dark:bg-slate-900">
                        <!-- Header -->
                        <div
                          class="bg-white dark:bg-slate-800 p-4 border-b border-gray-300 dark:border-gray-600 text-center text-gray-900 dark:text-white font-bold sticky top-0 z-10">
                          <div>{{ item | bookingTime }}</div>
                        </div>
                        <!-- Content -->
                        <div class="p-4 min-h-[200px]">
                          <app-court-availability-grid-column [column]="item"/>
                        </div>
                      </div>
                    }
                  }
                </div>
              </div>
            }
          }
        }
      </section>
  `,
  styles: [
    `
    `
  ]
})
export class CourtSpotterDashboardComponent implements OnInit {
  private courtAvailabilitiesApiService = inject(PadelCourtAvailabilitiesApiService);
  private padelClubsApiService = inject(PadelClubsApiService);
  private gridDataTransformService = inject(CourtAvailabilityDataTransformationService);
  private filtersService = inject(CourtAvailabilitiesSearchFilterService);
  private asyncStateService = inject(AsyncStateService);
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);

  courtAvailabilityData$?: Observable<AsyncState<CourtAvailabilityGridData>>;
  filters$?: Observable<CourtAvailabilitiesSearchFilters>;
  padelClubsState$?: Observable<AsyncState<PadelClub[]>>;

  date: Date = new Date();

  applyFilters(filters: CourtAvailabilitiesSearchFilters): void {
    const queryParams = CourtAvailabilitiesSearchFilterService.constructQueryParamsFromFilters(filters);
    void this.router.navigate(['/courts'], { queryParams });
  }

  private loadData(filters?: CourtAvailabilitiesSearchFilters): void {
    const selectedDurations = (filters && filters.duration) ? CourtAvailabilitiesSearchFilterService.getSelectedDurations(filters.duration) : undefined;
    const selectedClubIds = (filters && filters.clubIds) ? Array.from(filters.clubIds) : undefined;
    const date = (filters && filters.date) ? filters.date : new Date();
    const courtType = (filters && filters.courtType !== null) ? filters.courtType : undefined
    this.date = date;

    this.courtAvailabilityData$ = this.asyncStateService.wrapWithAsyncState(this.courtAvailabilitiesApiService.getAvailabilities(date, selectedDurations, selectedClubIds, courtType)
      .pipe(map(response => this.gridDataTransformService.transformToGridData(response)))
    )
  }

  ngOnInit(): void {
    this.filters$ = combineLatest([
      this.filtersService.filters$,
      this.route.queryParams
    ]).pipe(
      map(([filters, queryParams]) => {
        return CourtAvailabilitiesSearchFilterService.updateFiltersFromQueryParams(queryParams, filters);
      }),
      distinctUntilChanged()
    )

    this.filters$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((filters) => {
      this.loadData(filters);
    })

    this.padelClubsState$ = this.asyncStateService.wrapWithAsyncState(this.padelClubsApiService.getClubs().pipe(map(response => {
      return response.clubs.map(dto => ({
        name: dto.name,
        id: dto.clubId
      }))
    })));
  }
}
