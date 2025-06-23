import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bookingDuration'
})
export class BookingDurationPipe implements PipeTransform {
  transform(value: number): string {
    switch (value) {
      case 60:
        return '1 hour'
      case 90:
        return '1.5 hours'
      case 120:
        return '2 hours'
      default:
        return `${value} mins`
    }
  }
}
