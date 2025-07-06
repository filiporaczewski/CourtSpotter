import {Component, computed, input, model} from '@angular/core';
import {NgStyle} from '@angular/common';

@Component({
  selector: 'app-pc-overlay-dialog',
  imports: [
    NgStyle
  ],
  template: `
    @if(visible()) {
      <div class="fixed inset-0 flex items-center justify-center" [class.animate-fade-in]="visible()" (click)="close()">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-xs"></div>
        <div [ngStyle]="{width: overlayWidthPx()}" class="relative test-gray-200 border border-gray-200 bg-slate-950 rounded-lg shadow-xl w-full mx-4 max-h-[90vh] overflow-y-auto transform transition-all duration-300 ease-in-out" (click)="$event.stopPropagation()">
          <div>
            <ng-content />
          </div>
        </div>
      </div>
    }
  `
})
export class PcOverlayDialogComponent {
  visible = model.required<boolean>();
  overlayWidth = input<number>();

  close = () => this.visible.set(false)

  overlayWidthPx = computed(() => `${this.overlayWidth()}px`)
}
