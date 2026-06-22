import { Component, input } from '@angular/core';
import { StatusBadgeComponent } from '../status-badge/status-badge';
import { DateFormatPipe } from '../../pipes/date-format.pipe';

export interface TimelineItem {
  status: string;
  date: string;
  remarks?: string;
  changedBy?: string;
}

@Component({
  selector: 'app-timeline',
  standalone: true,
  imports: [StatusBadgeComponent, DateFormatPipe],
  templateUrl: './timeline.html',
})
export class TimelineComponent {
  items = input.required<TimelineItem[]>();
}
