import {Component, inject, OnInit} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {PadelCourtsDashboardComponent} from './components/dashboard/padel-courts-dashboard.component';
import {FooterComponent} from './components/footer/footer.component';
import {ThemeService} from './services/theme.service';
import {map} from 'rxjs';
import {AsyncPipe} from '@angular/common';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PadelCourtsDashboardComponent, FooterComponent, AsyncPipe],
  template: `
    @if(themeClass$ | async ; as themeClass) {
      <div [class]="themeClass">
        <div class="min-h-screen bg-white dark:bg-slate-800 text-gray-900 dark:text-gray-300">
          <app-padel-courts-dashboard />
          <app-footer />
          <router-outlet />
        </div>
      </div>
    }
  `
})
export class AppComponent implements OnInit {
  title = 'PadelCourtsSpa';
  private themeService = inject(ThemeService);
  themeClass$ = this.themeService.theme$.pipe(map(theme => {
    let prefersDark: boolean;

    if (theme === 'system') {
      prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    } else {
      prefersDark = theme === 'dark';
    }

    return prefersDark ? 'dark' : 'light';
  }))

  ngOnInit(): void {
    this.themeService.initializeTheme();
  }
}
