import { Pipe, PipeTransform } from '@angular/core';

const MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

@Pipe({ name: 'dateFormat', standalone: true })
export class DateFormatPipe implements PipeTransform {
  transform(value: string | null | undefined, format: 'short' | 'long' = 'short'): string {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d.getTime())) return value;
    const day = d.getDate().toString().padStart(2, '0');
    const month = MONTHS[d.getMonth()];
    const year = d.getFullYear();
    if (format === 'long') {
      const hours = d.getHours().toString().padStart(2, '0');
      const mins = d.getMinutes().toString().padStart(2, '0');
      return `${day} ${month} ${year}, ${hours}:${mins}`;
    }
    return `${day} ${month} ${year}`;
  }
}
