import { Pipe, PipeTransform } from '@angular/core';

@Pipe({ name: 'money', standalone: true })
export class MoneyPipe implements PipeTransform {
  transform(value: number | null | undefined, symbol = '₹'): string {
    if (value == null) return `${symbol} 0.00`;
    const formatted = this.indianFormat(Math.abs(value));
    return value < 0 ? `-${symbol} ${formatted}` : `${symbol} ${formatted}`;
  }

  private indianFormat(num: number): string {
    const [integer, decimal] = num.toFixed(2).split('.');
    const lastThree = integer.slice(-3);
    const rest = integer.slice(0, -3);
    const formatted = rest.replace(/\B(?=(\d{2})+(?!\d))/g, ',');
    return (formatted ? formatted + ',' : '') + lastThree + '.' + decimal;
  }
}
