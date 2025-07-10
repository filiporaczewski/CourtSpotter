import { Routes } from '@angular/router';
import {PadelCourtsDashboardComponent} from './components/dashboard/padel-courts-dashboard.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/courts',
    pathMatch: 'full'
  },
  {
    path: 'courts',
    component: PadelCourtsDashboardComponent
  },
  {
    path: '**',
    redirectTo: '/courts'
  }
];
