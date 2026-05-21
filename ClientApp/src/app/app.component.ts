import { Component } from '@angular/core';

@Component({
  selector: 'app-root',
  template: `
    <div class="app-container">
      <h1>AI Angular - News</h1>
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    .app-container {
      max-width: 1000px;
      margin: 0 auto;
      padding: 20px;
      font-family: Arial, sans-serif;
    }
    h1 {
      color: #333;
      margin-bottom: 20px;
      text-align: center;
    }
  `]
})
export class AppComponent {}
