import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {PadelCourtsDashboardComponent} from './components/padel-courts-dashboard.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PadelCourtsDashboardComponent],
  template: `
    <app-padel-courts-dashboard />
    <router-outlet />
  `
})
export class AppComponent {
  title = 'PadelCourtsSpa';
}
