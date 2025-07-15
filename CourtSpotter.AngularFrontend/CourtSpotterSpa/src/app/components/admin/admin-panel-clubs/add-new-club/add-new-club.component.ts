import {Component, DestroyRef, inject} from '@angular/core';
import {TranslocoPipe} from '@jsverse/transloco';
import {PROVIDER_TYPE_OPTIONS, ProviderType} from '../../../../models/provider-type';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {PadelClubsApiService} from '../../../../services/rest-api/padel-clubs/padel-clubs.api.service';
import {PadelClubAddCommand} from '../../../../models/padel-club-add-command';
import {AsyncStateService} from '../../../../shared/services/async-state.service';
import {takeUntilDestroyed} from '@angular/core/rxjs-interop';
import {CsLoaderComponent} from '../../../../shared/components/cs-loader/cs-loader.component';
import {Router} from '@angular/router';

@Component({
  selector: 'app-add-new-club',
  imports: [
    TranslocoPipe,
    ReactiveFormsModule,
    CsLoaderComponent
  ],
  template: `
    <section class="py-8">
      @if(loading) {
        <app-cs-loader />
      } @else {
        <h2 class="text-4xl text-center font-bold mb-6 text-gray-900 dark:text-gray-100">{{ 'admin_panel.clubs.add_new_page_title' | transloco }}</h2>
        <form (ngSubmit)="onSubmit()" [formGroup]="addClubForm" class="w-full p-6 bg-white dark:bg-slate-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-600">
          <div class="space-y-12 mx-4">
            <div class="form-group">
              <label for="clubName" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                {{ 'admin_panel.clubs.form.labels.club_name' | transloco }} <span class="text-red-500">*</span>
              </label>
              <input
                type="text"
                id="clubName"
                name="clubName"
                formControlName="name"
                class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-slate-700 dark:text-gray-100 dark:placeholder-gray-400 bg-white text-gray-900 transition-colors duration-200"
                [placeholder]="'admin_panel.clubs.form.placeholders.club_name' | transloco"
                [class.border-red-500]="addClubForm.get('name')?.invalid && addClubForm.get('name')?.touched"
              />
              @if (addClubForm.get('name')?.invalid && addClubForm.get('name')?.touched) {
                <p class="mt-1 text-sm text-red-600 dark:text-red-400">{{ 'admin_panel.validation_errors.club_name_required' | transloco }}</p>
              }
            </div>

            <div class="form-group">
              <label for="provider" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                {{ 'admin_panel.clubs.form.labels.provider' | transloco }} <span class="text-red-500">*</span>
              </label>
              <select
                formControlName="provider"
                id="provider"
                class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-slate-700 dark:text-gray-100 bg-white text-gray-900 transition-colors duration-200"
              >
                @for (option of providerOptions; track option.value) {
                  <option [value]="option.value">{{ option.displayValue }}</option>
                }
              </select>
            </div>

            <div class="form-group">
              <label for="pagesCount" class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                {{ 'admin_panel.clubs.form.labels.pages_count' | transloco }}
              </label>
              <input
                formControlName="pagesCount"
                type="number"
                id="pagesCount"
                name="pagesCount"
                min="1"
                class="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500 dark:bg-slate-700 dark:text-gray-100 dark:placeholder-gray-400 bg-white text-gray-900 transition-colors duration-200"
                [placeholder]="'admin_panel.clubs.form.placeholders.pages_count' | transloco"
              />
            </div>

            <div class="form-group flex justify-center items-center">
              <button
                type="submit"
                [disabled]="addClubForm.invalid"
                class="cursor-pointer font-semibold py-2 px-4 rounded-md transition-colors duration-200 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 focus:ring-offset-gray-50 dark:focus:ring-offset-slate-800 bg-blue-500 dark:bg-blue-600 hover:bg-blue-600 dark:hover:bg-blue-700 text-white disabled:bg-gray-400 disabled:dark:bg-gray-600 disabled:text-gray-200 disabled:dark:text-gray-400 disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-gray-400 disabled:dark:hover:bg-gray-600"
              >
                {{ 'admin_panel.clubs.form.buttons.add' | transloco }}
              </button>
            </div>
          </div>
        </form>
      }
    </section>
  `,
  styles: `
    :host {
      width: 100%;
    }
  `
})
export class AddNewClubComponent {
  providerOptions = PROVIDER_TYPE_OPTIONS;
  private fb = inject(FormBuilder);
  private clubsApiService = inject(PadelClubsApiService);
  private asyncStateService = inject(AsyncStateService);
  private destroyRef = inject(DestroyRef);
  private router = inject(Router);

  loading: boolean = false;

  addClubForm = this.fb.group({
    name: [null as string | null, [Validators.required]],
    provider: [null as ProviderType | null, [Validators.required]],
    pagesCount: [null as number | null]
  })

  onSubmit = () => {
    if (this.addClubForm.valid) {
      const formVal = this.addClubForm.value;

      const club: PadelClubAddCommand = {
        name: formVal.name!,
        pagesCount: formVal.pagesCount !== null ? formVal.pagesCount : undefined,
        provider: Number(formVal.provider!)
      }

      this.asyncStateService.wrapWithAsyncState(this.clubsApiService.addClub(club))
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe((addNewClubState) => {
          if (addNewClubState.loading) {
            this.loading = true;
          }

          if (addNewClubState.error) {
            alert(addNewClubState.error);
            this.loading = false;
            void this.router.navigate(['/admin/clubs']);
          }

          if (addNewClubState.data) {
            alert('New club added');
            void this.router.navigate(['/admin/clubs']);
          }
        })
    }
  }
}
