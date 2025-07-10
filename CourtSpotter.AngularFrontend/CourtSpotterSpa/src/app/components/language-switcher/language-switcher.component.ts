import {Component, inject} from '@angular/core';
import {TranslocoService} from '@jsverse/transloco';
import {NgClass, UpperCasePipe} from '@angular/common';

@Component({
  selector: 'app-language-switcher',
  imports: [
    UpperCasePipe,
    NgClass
  ],
  template: `
    <div class="flex gap-1">
      @for(lang of availableLangs; track lang) {
        <button (click)="changeLanguage(lang)"
            [ngClass]="{ '': isActive(lang) }"
            class="cursor-pointer px-2 py-1 rounded border hover:border-2 transition-colors dark:bg-slate-900 text-gray-800 dark:text-white text-sm"
        >
          {{ lang | uppercase }}
        </button>
      }
    </div>
  `,
  styles: ``
})
export class LanguageSwitcherComponent {
  private translocoService = inject(TranslocoService);
  availableLangs: string[] = this.translocoService.getAvailableLangs() as string[];

  changeLanguage = (lang: string) => this.translocoService.setActiveLang(lang);
  isActive = (lang: string): boolean => this.translocoService.getActiveLang() === lang;
}
