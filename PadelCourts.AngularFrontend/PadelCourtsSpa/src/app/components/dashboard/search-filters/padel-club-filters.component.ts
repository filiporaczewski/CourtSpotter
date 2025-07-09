import {Component, inject, input, model} from '@angular/core';
import {PadelClub} from '../../../models/padel-club';
import {NgIcon, provideIcons} from '@ng-icons/core';
import {heroInformationCircle} from '@ng-icons/heroicons/outline';
import {PcOverlayDialogComponent} from '../../../shared/pc-overlay-dialog/pc-overlay-dialog.component';
import {ClubInfoComponent} from '../../club-info/club-info.component';
import {AppStateService} from '../../../services/app-state.service';
import {TranslocoPipe} from '@jsverse/transloco';

@Component({
  selector: 'app-padel-club-filters',
  imports: [
    NgIcon,
    PcOverlayDialogComponent,
    ClubInfoComponent,
    TranslocoPipe
  ],
  providers: [provideIcons({ heroInformationCircle })],
  template: `
    <div class="mb-6">
      <h4 class="text-center md:text-left font-mono text-lg text-gray-900 dark:text-white mb-3">
        {{ 'filters.clubs' | transloco }}
      </h4>
      <div class="max-h-48 overflow-y-auto border border-gray-300 dark:border-gray-600 rounded-lg p-3 bg-white dark:bg-slate-800 max-w-96">
        @for (padelClub of padelClubs(); track padelClub.id) {
          <label class="flex items-center md:justify-normal justify-center space-x-2 font-mono text-gray-900 dark:text-white cursor-pointer py-1">
            <input type="checkbox" [checked]="isClubSelected(padelClub.id)" (change)="toggleClubSelection(padelClub.id, $event)" class="cursor-pointer w-3 h-3 md:w-4 md:h-4 text-blue-600 bg-gray-100 border-gray-300 rounded focus:ring-blue-500 focus:ring-2" />
            <span class="text-sm text-clip">{{padelClub.name}}</span>
            <ng-icon (click)="onClubInfoClicked(padelClub.name, $event)" name="heroInformationCircle" />
          </label>
        }
      </div>
    </div>
    <app-pc-overlay-dialog [(visible)]="clubInfoDialogVisible" [overlayWidth]="650">
      <app-club-info />
    </app-pc-overlay-dialog>
  `,
  styles: ``
})
export class PadelClubFiltersComponent {
  padelClubs = input.required<PadelClub[]>();
  clubIds = model.required<Set<string>>();
  clubInfoDialogVisible = model<boolean>(false);

  private appStateService = inject(AppStateService);

  toggleClubSelection = (clubId: string, event: Event) => {
    const target = event.target as HTMLInputElement;

    if (target.checked) {
      this.clubIds.update(currentSet => new Set([...currentSet, clubId]));
    } else {
      this.clubIds.update(currentSet => new Set([...currentSet].filter(id => id !== clubId)));
    }
  }

  isClubSelected = (clubId: string) => this.clubIds().has(clubId)

  onClubInfoClicked = (clubName: string, event: Event) => {
    event.stopPropagation();
    event.preventDefault();
    this.clubInfoDialogVisible.set(true);
    this.appStateService.updateSelectedClub(clubName)
  }
}
