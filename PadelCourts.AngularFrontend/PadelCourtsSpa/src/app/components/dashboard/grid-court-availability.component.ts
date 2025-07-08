import {Component, computed, input, Signal} from '@angular/core';
import {CourtAvailabilityGridItem} from '../../models/CourtAvailabilityGridModels';
import {CourtType} from '../../models/court-type';
import {PcTagComponent} from '../../shared/pc-tag/pc-tag.component';

@Component({
  selector: 'app-grid-court-availability',
  imports: [
    PcTagComponent
  ],
  template: `
    <div class="mb-3 border border-gray-300 dark:border-gray-600 rounded-md p-3 bg-white dark:bg-slate-800 whitespace-nowrap">
      <div class="font-bold font-mono text-gray-900 dark:text-gray-200">{{availability().courtName}}</div>
      <div class="py-1 flex items-center justify-center gap-1 mb-1 font-bold text-sm">
        <span class="px-0.5">
          @if(isIndoor()) {
            <app-pc-tag value="Indoor" backgroundColorClass="bg-gray-800 dark:bg-slate-950" textColorClass="text-white" />
          } @else {
            <app-pc-tag value="Outdoor" backgroundColorClass="bg-green-700 dark:bg-green-800" textColorClass="text-white" />
          }
        </span>

        @for (durationConfig of durationConfigs(); track $index) {
          <app-pc-tag [value]="durationConfig.text" [backgroundColorClass]="durationConfig.bgColorClass" [textColorClass]="durationConfig.textColorClass" paddingXClass="px-1.5" />
        }
      </div>
      <div><a [href]="availability().bookingUrl" target="_blank" class="text-center text-blue-600 dark:text-blue-400 hover:text-blue-800 dark:hover:text-blue-600 hover:underline transition-colors duration-200">Book</a></div>
    </div>
  `,
  styles: ``
})
export class GridCourtAvailabilityComponent {
  availability = input.required<CourtAvailabilityGridItem>()
  isIndoor = computed(() => this.availability().courtType === CourtType.Indoor);

  durationConfigs: Signal<durationConfig[]> = computed(() => {
    return this.availability().durationsInMinutes.map((duration) => {
      return this.getDurationConfig(duration);
    })
  })

  private getDurationConfig = (durationInMinutes: number): durationConfig => {
    switch (durationInMinutes) {
      case 60:
        return {
          text: '1h',
          bgColorClass: 'bg-blue-600 dark:bg-blue-600',
          textColorClass: 'text-white'
        }
      case 90:
        return {
          text: '1.5h',
          bgColorClass: 'bg-blue-600 dark:bg-blue-600',
          textColorClass: 'text-white'
        }
      default:
        return {
          text: '2h',
          bgColorClass: 'bg-blue-600 dark:bg-blue-600',
          textColorClass: 'text-white'
        }
    }
  }
}

interface durationConfig {
  text: string,
  bgColorClass: string,
  textColorClass: string
}
