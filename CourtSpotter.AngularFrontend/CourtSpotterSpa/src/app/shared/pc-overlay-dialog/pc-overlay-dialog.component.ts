import {Component, computed, input, model} from '@angular/core';
import {NgStyle} from '@angular/common';

@Component({
  selector: 'app-pc-overlay-dialog',
  imports: [
    NgStyle
  ],
  template: `
    @if(visible()) {
      <div class="z-50 fixed inset-0 flex items-center justify-center" [class.animate-fade-in]="visible()" (click)="close()">
        <div class="absolute inset-0 bg-black/50 backdrop-blur-xs"></div>
        <div [ngStyle]="{width: overlayWidthPx()}" class="relative test-gray-200 border border-gray-400 dark:border-gray-200  bg-white dark:bg-slate-800 rounded-lg shadow-xl w-full mx-4 max-h-[90vh] overflow-y-auto transform transition-all duration-300 ease-in-out animate-pulse-once" (click)="$event.stopPropagation()">
          <div>
            <ng-content />
          </div>
        </div>
      </div>
    }
  `,
  styles: `
    .animate-fade-in {
      animation: fadeIn 0.2s ease-out;
    }

    .animate-pulse-once {
      animation: pulse 0.3s ease-in-out;
    }

    @keyframes fadeIn {
      from {
        opacity: 0;
      }
      to {
        opacity: 1;
      }
    }

    @keyframes pulse {
      0% {
        opacity: 0.7;
        transform: scale(0.95);
      }
      50% {
        opacity: 1;
        transform: scale(1.02);
      }
      100% {
        opacity: 1;
        transform: scale(1);
      }
    }
  `

})
export class PcOverlayDialogComponent {
  visible = model.required<boolean>();
  overlayWidth = input<number>();

  close = () => this.visible.set(false)

  overlayWidthPx = computed(() => `${this.overlayWidth()}px`)
}
