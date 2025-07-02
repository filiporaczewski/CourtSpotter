import {Component, input, model} from '@angular/core';
import {PadelClub} from '../models/padel-club';

@Component({
  selector: 'app-padel-club-filters',
  imports: [],
  template: `
    <div class="mb-6">
      <h4 class="font-mono text-lg text-white mb-3">
        Clubs
      </h4>
      <div class="max-h-48 overflow-y-auto border border-gray-600 rounded-lg p-3 bg-slate-800 max-w-96">
        @for (padelClub of padelClubs(); track padelClub.id) {
          <label class="flex items-center space-x-2 font-mono text-white cursor-pointer py-1">
            <input type="checkbox" [checked]="isClubSelected(padelClub.id)" (change)="toggleClubSelection(padelClub.id, $event)" class="w-4 h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2" />
            <span class="text-sm">{{padelClub.name}}</span>
          </label>
        }
      </div>
    </div>
  `,
  styles: ``
})
export class PadelClubFiltersComponent {
  padelClubs = input.required<PadelClub[]>();
  clubIds = model.required<Set<string>>();

  toggleClubSelection = (clubId: string, event: Event) => {
    const target = event.target as HTMLInputElement;

    if (target.checked) {
      this.clubIds.update(currentSet => new Set([...currentSet, clubId]));
    } else {
      this.clubIds.update(currentSet => new Set([...currentSet].filter(id => id !== clubId)));
    }
  }

  isClubSelected = (clubId: string) => this.clubIds().has(clubId)
}
