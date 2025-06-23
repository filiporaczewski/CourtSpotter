import {Component, input} from '@angular/core';
import {CourtAvailabilityGridColumn} from '../models/CourtAvailabilityGridModels';
import {CourtAvailabilityGridClubComponent} from './court-availability-grid-club.component';

@Component({
  selector: 'app-court-availability-grid-column',
  imports: [
    CourtAvailabilityGridClubComponent
  ],
  template: `
    <section class="availability-column">
      @for (club of column().gridClubs; track club.clubName) {
        <app-court-availability-grid-club [club]="club" />
      }
    </section>
  `,
  styles: ``
})
export class CourtAvailabilityGridColumnComponent {
  column = input.required<CourtAvailabilityGridColumn>();
}
