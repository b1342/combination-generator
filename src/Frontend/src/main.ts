import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app/app.component';
import { appConfig } from './app/app.config';

// נקודת הכניסה: מפעילים את האפליקציה עם הרכיב הראשי וההגדרות שלה
bootstrapApplication(AppComponent, appConfig).catch((error: unknown) => console.error(error));
