import {Component, inject} from '@angular/core';
import {ThemeService} from '../../services/theme.service';
import {AsyncPipe, NgOptimizedImage} from '@angular/common';
import {LanguageSwitcherComponent} from '../language-switcher/language-switcher.component';
import {ThemePickerComponent} from '../theme-picker/theme-picker.component';
import {MsalService} from '@azure/msal-angular';
import {LoggedInUserService} from '../../services/logged-in-user.service';
import {TranslocoPipe} from '@jsverse/transloco';

@Component({
  selector: 'app-header',
  imports: [
    AsyncPipe,
    LanguageSwitcherComponent,
    ThemePickerComponent,
    NgOptimizedImage,
    TranslocoPipe
  ],
  template: `
    @if(logoSrc$ | async ; as logoSrc) {
      <section class="pt-6 flex justify-between items-end border-b border-gray-200 dark:border-gray-700 pb-2">
        <div>
          <img [ngSrc]="logoSrc" width="150" height="61" alt="logo" priority />
        </div>
        <div class="gap-3 flex">
          <app-language-switcher />
          <app-theme-picker />
          @if(loggedIn$ | async ; as loggedIn) {
            @if (loggedIn) {
              <button
                (click)="logout()" class="cursor-pointer text-blue-600 dark:text-blue-400 font-mono px-3 underline">
                {{ 'logout' | transloco }}
              </button>
            }
          }
        </div>
      </section>
    }
  `,
  styles: ``
})
export class HeaderComponent {
  private themeService = inject(ThemeService);
  private authService = inject(MsalService);
  private loggedInUserService = inject(LoggedInUserService);

  loggedIn$ = this.loggedInUserService.isLoggedIn$;

  logoSrc$ = this.themeService.logoSrc$;

  logout = () => {
    this.authService.logout();
  }
}
