import {Component, Input} from '@angular/core';
import {NgClass} from '@angular/common';

@Component({
  selector: 'app-pc-tag',
  imports: [
    NgClass
  ],
  template: `
    <span class="inline-flex items-center rounded-sm py-1.5 text-xs font-medium" [class]="defaultClasses">
      {{ value }}
    </span>
  `,
  styles: ``
})
export class PcTagComponent {
  @Input() value: string = '';
  @Input() backgroundColorClass = '';
  @Input() textColorClass = '';
  @Input() paddingXClass = '';

  get defaultClasses(): string {
    const backgroundColorClass = this.backgroundColorClass ?? 'bg-blue-100';
    const textColorClass = this.textColorClass ?? 'text-blue-800';
    const paddingXClass = this.paddingXClass === '' ? 'px-2.5' : this.paddingXClass;

    return `${backgroundColorClass} ${textColorClass} ${paddingXClass}`;
  }
}
