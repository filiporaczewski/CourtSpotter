import {Component, inject} from '@angular/core';
import {ThemeService} from '../../services/theme.service';
import {AsyncPipe, NgOptimizedImage} from '@angular/common';
import {LanguageSwitcherComponent} from '../language-switcher/language-switcher.component';
import {ThemePickerComponent} from '../theme-picker/theme-picker.component';

@Component({
  selector: 'app-header',
  imports: [
    AsyncPipe,
    LanguageSwitcherComponent,
    ThemePickerComponent,
    NgOptimizedImage
  ],
  template: `
    @if(logoSrc$ | async ; as logoSrc) {
      <section class="flex items-center justify-center pt-6">
        <div>
          <div class="mb-4">
            <img [ngSrc]="logoSrc" width="300" height="122" alt="logo" priority />
          </div>
          <div class="flex items-center justify-center gap-3">
            <app-language-switcher />
            <app-theme-picker />
          </div>
        </div>
      </section>
    }
  `,
  styles: ``
})
export class HeaderComponent {
  private themeService = inject(ThemeService);
  logoSrc$ = this.themeService.logoSrc$;
}
