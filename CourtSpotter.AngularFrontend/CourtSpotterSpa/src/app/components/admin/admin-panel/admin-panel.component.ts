import { Component } from '@angular/core';
import {AdminPanelNavComponent} from '../admin-panel-nav/admin-panel-nav.component';
import {AdminPanelClubsComponent} from '../admin-panel-clubs/admin-panel-clubs.component';
import {RouterOutlet} from '@angular/router';

@Component({
  selector: 'app-admin-panel',
  imports: [
    AdminPanelNavComponent,
    AdminPanelClubsComponent,
    RouterOutlet
  ],
  template: `
    <section class="flex min-h-[70vh] mt-2">
      <div class="md:w-fit md:min-w-[10%]">
        <app-admin-panel-nav />
      </div>
      <div class="md:w-full flex justify-center">
        <router-outlet />
      </div>
    </section>
  `,
  styles: ``
})
export class AdminPanelComponent {

}
