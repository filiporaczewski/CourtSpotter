import {Component} from '@angular/core';
import {TranslocoPipe} from '@jsverse/transloco';
import {RouterLink} from '@angular/router';

@Component({
  selector: 'app-admin-panel-nav',
  imports: [
    TranslocoPipe,
    RouterLink
  ],
  template: `
    <h3 class="text-xl md:text-2xl font-bold">{{ 'admin_panel.title' | transloco }}</h3>
    <ul class="hidden md:block">
      <li>
        <a routerLink="/admin/clubs" class="cursor-pointer text-blue-600 dark:text-blue-400 font-mono underline">{{ 'admin_panel.nav.clubs' | transloco }}</a>
      </li>
    </ul>
  `,
  styles: ``
})
export class AdminPanelNavComponent {

}
