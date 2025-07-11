import { Injectable } from '@angular/core';
import {catchError,  map, Observable, of, startWith, timeout} from 'rxjs';
import { AsyncState } from '../../models/AsyncState';

@Injectable({
  providedIn: 'root'
})
export class AsyncStateService {
  wrapWithAsyncState<T>(source$: Observable<T>, timeoutMs: number = 10000): Observable<AsyncState<T>> {
    return source$.pipe(
      timeout(timeoutMs),
      map(data => ({
        loading: false,
        data,
        error: null
      }) as AsyncState<T>),
      catchError(error =>
        of({
          loading: false,
          data: null,
          error: 'An error occurred' // todo serious error handling
        })
      ),
      startWith({
        loading: true,
        data: null,
        error: null
      } as AsyncState<T>),
    )
  }
}
