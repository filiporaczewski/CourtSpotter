import {Component, inject} from '@angular/core';
import {ThemeService} from '../../services/theme.service';
import {NgIcon, provideIcons} from '@ng-icons/core';
import {heroMoon, heroSun} from '@ng-icons/heroicons/outline';

@Component({
  selector: 'app-theme-picker',
  imports: [
    NgIcon
  ],
  providers: [provideIcons({ heroSun, heroMoon })],
  template: `
    <div class="flex items-center justify-center gap-1">
      <div class="cursor-pointer rounded border dark:border-gray-300 dark:bg-slate-900 border-gray-900 bg-white p-1 flex items-center justify-center hover:border-2" (click)="switchToLight()">
        <ng-icon name="heroSun" size="20px" />
      </div>
      <div class="cursor-pointer rounded border dark:border-gray-300 dark:bg-slate-900 border-gray-900 bg-white p-1 flex items-center justify-center hover:border-2" (click)="switchToDark()">
        <ng-icon name="heroMoon" size="20px" />
      </div>
    </div>
  `,
  styles: ``
})
export class ThemePickerComponent {
  private themeService = inject(ThemeService);
  switchToDark = () => this.themeService.setTheme('dark')
  switchToLight = () => this.themeService.setTheme('light')
}
