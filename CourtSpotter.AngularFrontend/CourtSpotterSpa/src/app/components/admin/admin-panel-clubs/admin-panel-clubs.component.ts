import {Component, inject} from '@angular/core';
import {TranslocoPipe} from '@jsverse/transloco';
import {PadelClubsApiService} from '../../../services/rest-api/padel-clubs/padel-clubs.api.service';
import {AsyncStateService} from '../../../shared/services/async-state.service';
import {map, Observable} from 'rxjs';
import {AsyncState} from '../../../models/AsyncState';
import {PadelClubAdmin} from '../../../models/PadelClubAdmin';
import {AsyncPipe} from '@angular/common';
import {CsLoaderComponent} from '../../../shared/components/cs-loader/cs-loader.component';
import {RouterLink} from '@angular/router';
import {ErrorBoxComponent} from '../../../shared/components/error-box/error-box.component';

@Component({
  selector: 'app-admin-panel-clubs',
  imports: [
    TranslocoPipe,
    AsyncPipe,
    CsLoaderComponent,
    RouterLink,
    ErrorBoxComponent
  ],
  template: `
    <div class="flex justify-center items-center">
      <h1 class="font-bold text-4xl p-6 text-center mb-2">
        {{ 'admin_panel.clubs.title' | transloco }}
      </h1>
      <button routerLink="/admin/clubs/add-new" class="cursor-pointer dark:bg-blue-600 bg-green-500 hover:bg-green-600 dark:hover:bg-blue-700 text-white font-mono font-semibold py-2 px-6 rounded-lg transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-gray-50 dark:focus:ring-offset-slate-800">
        {{ 'admin_panel.actions.add' | transloco }}
      </button>
    </div>

    @if(padelClubsState$ | async ; as padelClubsState) {
      @if(padelClubsState.loading) {
        <app-cs-loader />
      }

      @if(padelClubsState.error) {
        <app-error-box />
      }

      @if(padelClubsState.data ; as padelClubs) {
        <div class="bg-white dark:bg-slate-800 border border-gray-300 dark:border-gray-600 rounded-lg overflow-hidden shadow-sm">
          <table class="w-full text-center">
            <thead class="bg-gray-50 dark:bg-slate-700">
            <tr>
              <th class="py-3 px-4 text-gray-900 dark:text-white font-semibold text-sm uppercase tracking-wider border-b border-gray-200 dark:border-gray-600">
                {{ 'admin_panel.clubs.table_headers.name' | transloco }}
              </th>
              <th class="py-3 px-4 text-gray-900 dark:text-white font-semibold text-sm uppercase tracking-wider border-b border-gray-200 dark:border-gray-600">
                {{ 'admin_panel.clubs.table_headers.club_id' | transloco }}
              </th>
              <th class="py-3 px-4 text-gray-900 dark:text-white font-semibold text-sm uppercase tracking-wider border-b border-gray-200 dark:border-gray-600">
                {{ 'admin_panel.clubs.table_headers.provider' | transloco }}
              </th>
              <th class="py-3 px-4 text-gray-900 dark:text-white font-semibold text-sm uppercase tracking-wider border-b border-gray-200 dark:border-gray-600">
                {{ 'admin_panel.clubs.table_headers.pages_count' | transloco }}
              </th>
            </tr>
            </thead>
            <tbody class="divide-y divide-gray-200 dark:divide-gray-600">
              @for(club of padelClubs ; track club.id) {
                <tr class="hover:bg-gray-50 dark:hover:bg-slate-700 transition-colors duration-150">
                  <td class="py-3 px-4 text-gray-900 dark:text-gray-300">{{ club.name }}</td>
                  <td class="py-3 px-4 text-gray-600 dark:text-gray-400 font-mono text-sm">{{ club.id }}</td>
                  <td class="py-3 px-4 text-gray-900 dark:text-gray-300">{{ club.provider }}</td>
                  <td class="py-3 px-4 text-gray-600 dark:text-gray-400">{{ club.pagesCount }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

      }
    }
  `,
  styles: `
    :host {
      width: 100%;
    }

    table {
      th, td {
        min-width: 250px;
        padding: 1rem
      }
    }
  `
})
export class AdminPanelClubsComponent {
  private padelClubsApiService = inject(PadelClubsApiService);
  private asyncStateService = inject(AsyncStateService);

  padelClubsState$: Observable<AsyncState<PadelClubAdmin[]>> = this.asyncStateService.wrapWithAsyncState(
    this.padelClubsApiService.getClubs().pipe(map(response => {
      return response.clubs.map(c => {
        return {
          id: c.clubId,
          name: c.name,
          provider: c.provider,
          pagesCount: c.pagesCount
        }
      })
    }))
  )
}
