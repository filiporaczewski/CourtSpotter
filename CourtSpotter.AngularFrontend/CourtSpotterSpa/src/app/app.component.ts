import {Component, inject, OnInit} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {FooterComponent} from './components/footer/footer.component';
import {ThemeService} from './services/theme.service';
import {map} from 'rxjs';
import {AsyncPipe} from '@angular/common';
import {HeaderComponent} from './components/header/header.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, FooterComponent, AsyncPipe, HeaderComponent],
  template: `
    @if(themeClass$ | async ; as themeClass) {
      <div [class]="themeClass">
        <div class="min-h-screen bg-white dark:bg-slate-800 text-gray-900 dark:text-gray-300">
          <app-header />
          <router-outlet />
          <app-footer />
        </div>
      </div>
    }
  `
})
export class AppComponent implements OnInit {
  private themeService = inject(ThemeService);
  themeClass$ = this.themeService.theme$;

  ngOnInit(): void {
    this.themeService.initializeTheme();
  }
}
