import {inject, Injectable, OnDestroy} from '@angular/core';
import {MsalBroadcastService, MsalService} from '@azure/msal-angular';
import {BehaviorSubject, filter, Subject, takeUntil} from 'rxjs';
import {InteractionStatus} from '@azure/msal-browser';

@Injectable({
  providedIn: 'root'
})
export class LoggedInUserService implements OnDestroy {
  private msalBroadcastService = inject(MsalBroadcastService);
  private msalService = inject(MsalService);
  private isLoggedInSubject = new BehaviorSubject<boolean>(false);
  isLoggedIn$ = this.isLoggedInSubject.asObservable();
  destroy$ = new Subject<void>();

  constructor() {
    this.msalBroadcastService.inProgress$.pipe(
      filter((status: InteractionStatus) => status === InteractionStatus.None),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      const accounts = this.msalService.instance.getAllAccounts();
      console.log(accounts);
      const isLoggedIn = accounts.length > 0;
      this.isLoggedInSubject.next(isLoggedIn);
    })
  }

  ngOnDestroy(): void {
    this.destroy$.next();
  }
}
