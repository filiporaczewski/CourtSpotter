import { FormsModule } from '@angular/forms';
import {Component, computed, input, model} from '@angular/core';

@Component({
  selector: 'app-date-filter',
  imports: [
    FormsModule
  ],
  template: `
    <div class="mb-6 max-w-96">
      <label class="font-mono text-lg text-gray-900 dark:text-white mb-3 block">
        Select Date
      </label>
      <input
        type="date"
        [ngModel]="selectedDateAsISOString()"
        [min]="minDate()"
        [max]="maxDate()"
        (ngModelChange)="onModelChange($event)"
        class="w-full px-3 py-2 bg-white dark:bg-slate-800 border border-gray-300 dark:border-gray-600 rounded-lg text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
      />
    </div>
  `,
  styles: ``
})
export class DateFilterComponent {
  today = new Date();

  selectedDate = model.required<Date>();
  maxDaysAhead = input.required<number>();

  minDate = computed(() => {
    return this.formatDate(this.today);
  });

  maxDate = computed(() => {
    const maxDate = new Date(this.today);
    maxDate.setDate(maxDate.getDate() + this.maxDaysAhead());
    return this.formatDate(maxDate);
  });

  selectedDateAsISOString = computed(() => this.formatDate(this.selectedDate()))

  onModelChange = (dateString: string) => {
    this.selectedDate.update(() => new Date(dateString))
  }

  private formatDate(date: Date): string {
    return date.toISOString().split('T')[0];
  }
}
