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
      <div [class]="themeClass" class="w-full flex justify-center bg-white dark:bg-slate-800 text-gray-900 dark:text-gray-300 font-mono">
        <div class="min-h-screen w-[90%] md:w-[60%] flex flex-col">
          <app-header class="w-full" />
          <router-outlet class="w-full" />
          <app-footer class="w-full" />
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
