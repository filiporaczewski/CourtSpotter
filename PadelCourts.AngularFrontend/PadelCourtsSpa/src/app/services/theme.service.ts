import { Injectable } from '@angular/core';
import {BehaviorSubject} from 'rxjs';

export type Theme = 'light' | 'dark' | 'system';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly THEME_KEY = 'theme-preference';
  private themeSubject = new BehaviorSubject<Theme>('system');
  theme$ = this.themeSubject.asObservable();

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
