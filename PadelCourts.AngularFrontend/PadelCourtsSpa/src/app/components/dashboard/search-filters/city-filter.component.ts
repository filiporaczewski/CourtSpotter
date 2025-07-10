import { Component } from '@angular/core';
import {TranslocoPipe} from '@jsverse/transloco';

@Component({
  selector: 'app-city-filter',
  imports: [
    TranslocoPipe
  ],
  template: `
    <div class="mb-6">
      <div class="flex mb-3 gap-1 items-center justify-center md:justify-normal md:text-left font-mono ">
        <h4 class="text-lg text-gray-900 dark:text-white">{{ 'filters.city' | transloco }}</h4>
      </div>
      <div class="cursor-not-allowed gap-1 flex justify-center items-center bg-white dark:bg-slate-800 max-w-96 border border-gray-300 dark:border-gray-600 rounded-lg p-2">
        <p>{{ 'filters.city_hardcoded' | transloco }}</p>
        <p class="text-xs text-gray-400">{{ 'filters.city_placeholder' | transloco }}</p>
      </div>
    </div>
  `,
  styles: ``
})
export class CityFilterComponent {

}
