import {Component, model} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {DurationFilters} from '../../../models/duration-filters';

@Component({
  selector: 'app-duration-filters',
  imports: [
    FormsModule
  ],
  template: `
    <div class="mb-6">
      <h4 class="font-mono text-lg text-gray-900 dark:text-white mb-3">Duration</h4>
      <div class="flex flex-wrap gap-4 bg-gray-100 dark:bg-slate-800 max-w-96 border border-gray-300 dark:border-gray-600 rounded-lg p-3">
        <label class="flex items-center space-x-2 font-mono text-gray-900 dark:text-white cursor-pointer">
          <input type="checkbox" [(ngModel)]="durationFilter().duration60" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2">
          <span>1h</span>
        </label>

        <label class="flex items-center space-x-2 font-mono text-gray-900 dark:text-white cursor-pointer">
          <input type="checkbox" [(ngModel)]="durationFilter().duration90" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2">
          <span>1.5h</span>
        </label>

        <label class="flex items-center space-x-2 font-mono text-gray-900 dark:text-white cursor-pointer">
          <input type="checkbox" [(ngModel)]="durationFilter().duration120" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2">
          <span>2h</span>
        </label>
      </div>
    </div>
  `,
  styles: ``
})
export class DurationFiltersComponent {
  durationFilter = model.required<DurationFilters>();
}
