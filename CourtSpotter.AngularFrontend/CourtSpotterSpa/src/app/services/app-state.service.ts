import { Injectable } from '@angular/core';
import {BehaviorSubject} from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AppStateService {
  private selectedClubSubject = new BehaviorSubject<string | null>(null);
  selectedClub$ = this.selectedClubSubject.asObservable();

  updateSelectedClub = (val: string | null) => {
    this.selectedClubSubject.next(val);
  }
}
