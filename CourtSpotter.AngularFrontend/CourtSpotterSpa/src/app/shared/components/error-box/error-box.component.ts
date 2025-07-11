import {Component, input} from '@angular/core';
import {NgIcon, provideIcons} from '@ng-icons/core';
import {heroExclamationTriangleSolid} from '@ng-icons/heroicons/solid';
import {TranslocoPipe} from '@jsverse/transloco';

@Component({
  selector: 'app-error-box',
  imports: [
    NgIcon,
    TranslocoPipe
  ],
  providers: [
    provideIcons({ heroExclamationTriangleSolid })
  ],
  template: `
    <div class="flex justify-center items-center font-mono font-gray-800 dark:font-white">
      <div class="flex flex-col items-center">
        @if(displayIcon()) {
          <ng-icon name="heroExclamationTriangleSolid" color="#ef4444" size="80" />
        }

        <!-- todo better error messages -->
        <p class="text-xl p-2">{{ 'errors.generic_error_message' | transloco }}</p>
        <p class="text-sm">{{ 'errors.try_again' | transloco }}</p>
      </div>
    </div>
  `,
  styles: ``
})
export class ErrorBoxComponent {
  displayIcon = input<boolean>(false)
}
