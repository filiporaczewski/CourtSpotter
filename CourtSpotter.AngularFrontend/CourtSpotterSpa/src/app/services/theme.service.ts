import { Injectable } from '@angular/core';
import {BehaviorSubject, map} from 'rxjs';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'theme-preference';
  private themeSubject = new BehaviorSubject<Theme>('system');
  theme$ = this.themeSubject.asObservable();

  themeClass$ = this.theme$.pipe(map(theme => {
    let prefersDark: boolean;

    if (theme === 'system') {
      prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    } else {
      prefersDark = theme === 'dark';
    }

    return prefersDark ? 'dark' : 'light';
  }));

  logoSrc$ = this.themeClass$.pipe(map(theme => {
    if (theme === 'light') {
      return 'images/court_spotter_logo_light.png'
    }

    return 'images/court_spotter_logo_dark.png'
  }));

  initializeTheme = (): void => {
    const savedTheme = localStorage.getItem(this.THEME_KEY) as Theme;

    if (savedTheme) {
      this.setTheme(savedTheme);
    } else {
      this.setTheme('system');
    }
  }

  setTheme = (value: Theme): void => {
    this.themeSubject.next(value);
    localStorage.setItem(this.THEME_KEY, value);
  }
}
