import {Component, input} from '@angular/core';
import {CourtAvailabilityGridClub} from '../models/CourtAvailabilityGridModels';
import {GridCourtAvailabilityComponent} from './grid-court-availability.component';

@Component({
  selector: 'app-court-availability-grid-club',
  imports: [
    GridCourtAvailabilityComponent
  ],
  template: `
    <div class="">
      <h4 class="text-xl font-bold text-white whitespace-nowrap mb-2">{{club().clubName}}</h4>
      @for (availability of club().availabilities; track availability.courtName) {
        <app-grid-court-availability [availability]="availability" />
      }
    </div>
  `,
  styles: ``
})
export class CourtAvailabilityGridClubComponent {
  club = input.required<CourtAvailabilityGridClub>()
}
