import {Component, inject, OnInit} from '@angular/core';
import {CourtAvailabilityGridData} from '../models/CourtAvailabilityGridModels';
import {map, Observable} from 'rxjs';
import {PadelCourtAvailabilitiesApiService} from '../services/padel-court-availabilities.api.service';
import {CourtAvailabilityDataTransformationService} from '../services/court-availability-data-transformation.service';
import {AsyncPipe, DatePipe} from '@angular/common';
import {CourtAvailabilityGridColumnComponent} from './court-availability-grid-column.component';

@Component({
  selector: 'app-padel-courts-dashboard',
  imports: [
    AsyncPipe,
    CourtAvailabilityGridColumnComponent,
    DatePipe
  ],
  template: `
    @if (data$ | async; as data) {
      <section class="">
        <div class="">
          <h2 class="font-mono text-2xl font-bold p-8">Padel courts available at {{ today | date  }}</h2>
          <table class="table-auto font-mono border-collapse border border-white bg-slate-950 mx-8">
            <thead class="bg-slate-700">
              <tr>
                @for (item of data.items; track item.startHour) {
                  <th class="border border-white p-4 text-white">{{item.startHour}}:00</th>
                }
              </tr>
            </thead>
            <tbody>
            <tr>
              @for (item of data.items; track item.startHour) {
                <td class="border border-white p-4">
                  <app-court-availability-grid-column [column]="item" />
                </td>
              }
            </tr>
            </tbody>
          </table>
        </div>
      </section>
    }
  `,
  styles: [
    `
      //.court-availabilities-container {
      //  display: flex;
      //  justify-content: center;
      //
      //  .availabilities-grid-title {
      //    margin-bottom: 3rem;
      //  }
      //
      //  .availabilities-grid {
      //    td, th {
      //      padding: 1rem;
      //      border: 1px solid black;
      //    }
      //  }
      //}
    `
  ]
})
export class PadelCourtsDashboardComponent implements OnInit {
  private apiService = inject(PadelCourtAvailabilitiesApiService);
  private gridDataTransformService = inject(CourtAvailabilityDataTransformationService);
  data$?: Observable<CourtAvailabilityGridData>;
  today: Date = new Date();

  ngOnInit(): void {
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    this.data$ = this.apiService.getAvailabilities(tomorrow).pipe(map(response => this.gridDataTransformService.transformToGridData(response)));
  }

}
