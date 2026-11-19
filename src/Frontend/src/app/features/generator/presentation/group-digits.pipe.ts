import { Pipe, PipeTransform } from '@angular/core';

/**
 * מוסיף פסיקים בין כל שלוש ספרות: "2432902008176640000" → "2,432,902,008,176,640,000".
 * עובד על הטקסט עצמו ולא ממיר למספר, כדי שמספרים גדולים לא יתעגלו.
 */
@Pipe({ name: 'groupDigits' })
export class GroupDigitsPipe implements PipeTransform {
  transform(value: string | null | undefined): string {
    if (!value) {
      return '';
    }

    // בכל מקום שאחריו יש קבוצות שלמות של 3 ספרות עד הסוף, מכניסים פסיק
    return value.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
  }
}
