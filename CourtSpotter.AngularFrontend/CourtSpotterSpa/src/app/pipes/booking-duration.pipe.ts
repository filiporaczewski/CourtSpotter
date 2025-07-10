import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'bookingDuration'
})
export class BookingDurationPipe implements PipeTransform {
  transform(value: number): string {
    switch (value) {
      case 60:
        return '1h'
      case 90:
        return '1.5h'
      case 120:
        return '2h'
      default:
        return `${value} mins`
    }
  }
}
