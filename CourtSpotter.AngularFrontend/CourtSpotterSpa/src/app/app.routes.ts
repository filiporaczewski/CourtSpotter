import { Routes } from '@angular/router';
import {CourtSpotterDashboardComponent} from './components/dashboard/court-spotter-dashboard.component';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/courts',
    pathMatch: 'full'
  },
  {
    path: 'courts',
    component: CourtSpotterDashboardComponent
  },
  {
    path: '**',
    redirectTo: '/courts'
  }
];
