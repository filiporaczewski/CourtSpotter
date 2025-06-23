import {Component, input} from '@angular/core';
import {CourtAvailabilityGridItem} from '../models/CourtAvailabilityGridModels';
import {BookingDurationPipe} from '../pipes/booking-duration.pipe';

@Component({
  selector: 'app-grid-court-availability',
  imports: [
    BookingDurationPipe
  ],
  template: `
    <div class="mb-3 border border-gray-300 rounded-md p-3 bg-slate-700 whitespace-nowrap">
      <div class="flex justify-between items-center"><span class="font-bold">{{availability().courtName}}</span><span class="text-sm text-yellow-300">{{availability().durationInMinutes | bookingDuration}}</span></div>
      <div class="font-bold text-sm mb-1">{{availability().price }}&nbsp;{{availability().currency}}</div>
      <a href="#" class="text-center text-blue-400 hover:text-blue-600 hover:underline transition-colors duration-200">Book in {{availability().provider }}</a>
    </div>
  `,
  styles: ``
})
export class GridCourtAvailabilityComponent {
  availability = input.required<CourtAvailabilityGridItem>()
}
