import {Component, inject, signal} from '@angular/core';
import {AppStateService} from '../../services/app-state.service';
import {AsyncPipe} from '@angular/common';
import {DomSanitizer} from '@angular/platform-browser';
import {environment} from '../../../environments/environment';

@Component({
  selector: 'app-club-info',
  imports: [
    AsyncPipe
  ],
  template: `
    <div class="text-center font-mono">
      <div class="flex justify-center">
        @if(selectedClub$ | async ; as selectedClub) {
          @if(selectedClub !== null) {
            <iframe
              width="650"
              height="450"
              style="border:0"
              loading="lazy"
              allowfullscreen
              referrerpolicy="no-referrer-when-downgrade"
              [src]="getGoogleMapsSrc(selectedClub)"
              >
            </iframe>
          }
        }
      </div>
    </div>
  `,
  styles: ``
})
export class ClubInfoComponent {
  private readonly apiKey = environment.googleMapsApiKey;
  private appStateService = inject(AppStateService);
  private sanitizer = inject(DomSanitizer);

  selectedClub$ = this.appStateService.selectedClub$;

  getGoogleMapsSrc = (clubName: string) => {
    const url = `https://www.google.com/maps/embed/v1/place?key=${this.apiKey}&q=${clubName}`;
    return this.sanitizer.bypassSecurityTrustResourceUrl(url);
  }
}
