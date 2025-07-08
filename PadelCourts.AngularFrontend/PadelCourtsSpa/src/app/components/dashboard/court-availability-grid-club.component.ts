import {Component, computed, input, signal} from '@angular/core';
import {CourtAvailabilityGridClub} from '../../models/CourtAvailabilityGridModels';
import {GridCourtAvailabilityComponent} from './grid-court-availability.component';

@Component({
  selector: 'app-court-availability-grid-club',
  imports: [
    GridCourtAvailabilityComponent
  ],
  template: `
    <div class="text-center">
      <h4 class="text-l font-bold font-mono text-gray-900 dark:text-white whitespace-nowrap mb-2">{{club().clubName}}</h4>

      @if(club().availabilities.length <= visibleAvailabilitiesCount) {
        @for (availability of club().availabilities; track $index) {
          <app-grid-court-availability [availability]="availability" />
        }
      } @else {
        @if(!isExpanded()){
          @for (availability of club().availabilities.slice(0, visibleAvailabilitiesCount); track $index) {
            <app-grid-court-availability [availability]="availability" />
          }
          <button
            class="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 text-sm mt-2 cursor-pointer underline mb-2"
            (click)="toggleExpanded()">
            ...{{excessiveAvailabilitiesCount()}} more availabilities
          </button>
        } @else {
          @for (availability of club().availabilities; track $index) {
            <app-grid-court-availability [availability]="availability" />
          }
          <button
            class="text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-300 text-sm mt-2 cursor-pointer underline mb-2"
            (click)="toggleExpanded()">
            Show less
          </button>
        }
      }
    </div>
  `,
  styles: ``
})
export class CourtAvailabilityGridClubComponent {
  club = input.required<CourtAvailabilityGridClub>()
  isExpanded = signal(false);
  visibleAvailabilitiesCount = 3;
  excessiveAvailabilitiesCount = computed(() => this.club().availabilities.length - this.visibleAvailabilitiesCount);
  toggleExpanded = () => this.isExpanded.update(value => !value);
}
