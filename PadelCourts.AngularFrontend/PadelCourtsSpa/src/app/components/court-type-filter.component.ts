import {Component, computed, model} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {CourtType} from '../models/court-type';

@Component({
  selector: 'app-court-type-filter',
  imports: [
    FormsModule
  ],
  template: `
    <div class="mb-6">
      <h4 class="font-mono text-lg text-white mb-3">Court type</h4>
      <div class="flex flex-wrap gap-4 bg-slate-800 border border-gray-600 rounded-lg p-3">
        <label class="flex items-center space-x-2 font-mono text-white cursor-pointer">
          <input type="checkbox" [checked]="isIndoor()" (change)="toggleIndoor($event)" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2" />
          <span>Indoor</span>
        </label>
        <label class="flex items-center space-x-2 font-mono text-white cursor-pointer">
          <input type="checkbox" [checked]="isOutdoor()" (change)="toggleOutdoor($event)" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2" />
          <span>Outdoor</span>
        </label>
      </div>
    </div>
  `,
  styles: ``
})
export class CourtTypeFilterComponent {
  courtType = model.required<CourtType | null>()
  isIndoor = computed(() => this.courtType() === CourtType.Indoor)
  isOutdoor = computed(() => this.courtType() === CourtType.Outdoor)

  toggleIndoor = (event: Event) => {
    const target = event.target as HTMLInputElement;

    if (target.checked) {
      this.courtType.update(() => CourtType.Indoor);
    } else {
      this.courtType.update(() => null);
    }
  }

  toggleOutdoor = (event: Event) => {
    const target = event.target as HTMLInputElement;

    if (target.checked) {
      this.courtType.update(() => CourtType.Outdoor);
    } else {
      this.courtType.update(() => null);
    }
  }
}
