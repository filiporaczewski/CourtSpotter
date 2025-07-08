import { Component } from '@angular/core';
import {ThemePickerComponent} from '../theme-picker/theme-picker.component';

@Component({
  selector: 'app-footer',
  imports: [
    ThemePickerComponent
  ],
  template: `
    <footer class="text-gray-600 dark:text-white mt-auto w-full">
      <div class="px-4 w-full flex items-center justify-center">
          <div class="p-6 w-4/5 text-center border-t border-gray-300 dark:border-gray-700 flex justify-center items-center gap-4">
            <p class="text-gray-500 dark:text-gray-400 text-sm">
              © {{ currentYear }} All rights reserved.
            </p>
            <app-theme-picker />
        </div>
      </div>
    </footer>
  `
})
export class FooterComponent {
  currentYear = new Date().getFullYear();
}
