import { Component } from '@angular/core';
import {ThemePickerComponent} from '../theme-picker/theme-picker.component';
import {LanguageSwitcherComponent} from '../language-switcher/language-switcher.component';
import {TranslocoPipe} from '@jsverse/transloco';

@Component({
  selector: 'app-footer',
  imports: [
    ThemePickerComponent,
    LanguageSwitcherComponent,
    TranslocoPipe
  ],
  template: `
    <footer class="text-gray-600 dark:text-white mt-auto w-full">
      <div class="px-4 w-full flex items-center justify-center">
          <div class="p-6 w-4/5 text-center border-t border-gray-300 dark:border-gray-700 flex justify-center items-center gap-4">
            <p class="text-gray-500 dark:text-gray-400 text-sm">
              © {{ currentYear }} {{ 'footer.copyright' | transloco }}
            </p>
            <app-language-switcher />
            <app-theme-picker />
        </div>
      </div>
    </footer>
  `
})
export class FooterComponent {
  currentYear = new Date().getFullYear();
}
