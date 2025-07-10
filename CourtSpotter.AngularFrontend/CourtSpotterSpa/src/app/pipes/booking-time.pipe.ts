import { Pipe, PipeTransform } from '@angular/core';
import {CourtAvailabilityGridColumn} from '../models/CourtAvailabilityGridModels';

@Pipe({
  name: 'bookingTime'
})
export class BookingTimePipe implements PipeTransform {

  transform(value: CourtAvailabilityGridColumn, ...args: unknown[]): string {
    const suffix: string = value.isHalfHour ? '30' : '00';
    return `${value.startHour}:${suffix}`;
  }

}
