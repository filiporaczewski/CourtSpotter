import {Component, input, model, output} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {CourtAvailabilitiesSearchFilters} from '../../../models/court-availabilities-search-filters';
import {PadelClub} from '../../../models/padel-club';
import {DurationFiltersComponent} from './duration-filters.component';
import {PadelClubFiltersComponent} from './padel-club-filters.component';
import {DateFilterComponent} from './date-filter.component';
import {NgIcon, provideIcons} from '@ng-icons/core';
import { heroAdjustmentsHorizontal } from '@ng-icons/heroicons/outline';
import {CourtTypeFilterComponent} from './court-type-filter.component';

@Component({
  selector: 'app-padel-courts-search-filters',
  imports: [
    FormsModule,
    DurationFiltersComponent,
    PadelClubFiltersComponent,
    DateFilterComponent,
    NgIcon,
    CourtTypeFilterComponent
  ],
  providers: [provideIcons({ heroAdjustmentsHorizontal })],
  template: `
    <div class="px-4 md:px-8 pb-6">
      <div class="py-6">
        <div class="flex justify-center items-center mb-4">
          <ng-icon name="heroAdjustmentsHorizontal" size="32px" color="#6a7282"></ng-icon>
          <h3 class="font-mono text-xl font-semibold text-gray-900 dark:text-white px-1">
            Filters
          </h3>
        </div>
        <div class="border border-gray-300 dark:border-gray-300 bg-gray-100 dark:bg-slate-900 p-4 md:p-6 rounded-lg w-fit">
          <div class="flex flex-col md:flex-row md:gap-6">
            <app-date-filter [maxDaysAhead]="maxDaysAhead()" [(selectedDate)]="filters().date" />
            <app-duration-filters [(durationFilter)]="filters().duration" />
            <app-padel-club-filters [(clubIds)]="filters().clubIds" [padelClubs]="padelClubs()" />
            <app-court-type-filter [(courtType)]="filters().courtType" />
          </div>
          <div class="flex justify-center">
            <button
              (click)="applyFilters()" class="cursor-pointer dark:bg-blue-600 bg-green-500 hover:bg-green-600 dark:hover:bg-blue-700 text-white font-mono font-semibold py-2 px-6 rounded-lg transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-gray-50 dark:focus:ring-offset-slate-800">
              Apply Filters
            </button>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: ``
})
export class PadelCourtsSearchFiltersComponent {
  filters = model.required<CourtAvailabilitiesSearchFilters>();
  filtersApplied = output<CourtAvailabilitiesSearchFilters>();
  padelClubs = input.required<PadelClub[]>();
  maxDaysAhead = input.required<number>();

  applyFilters = () => {
    this.filtersApplied.emit(this.filters());
  }
}
