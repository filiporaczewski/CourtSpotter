import { Routes } from '@angular/router';
import {CourtSpotterDashboardComponent} from './components/dashboard/court-spotter-dashboard.component';
import {AdminPanelComponent} from './components/admin/admin-panel/admin-panel.component';
import {MsalGuard} from '@azure/msal-angular';
import {AddNewClubComponent} from './components/admin/admin-panel-clubs/add-new-club/add-new-club.component';
import {AdminPanelClubsComponent} from './components/admin/admin-panel-clubs/admin-panel-clubs.component';

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
    path: 'admin',
    component: AdminPanelComponent,
    canActivate: [MsalGuard],
    children: [
      {
        path: '',
        redirectTo: 'clubs',
        pathMatch: 'full'
      },
      {
        path: 'clubs',
        component: AdminPanelClubsComponent
      },
      {
        path: 'clubs/add-new',
        component: AddNewClubComponent
      }
    ]
  },
  {
    path: '**',
    redirectTo: '/courts'
  },
];
