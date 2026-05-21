import { Component, OnInit } from '@angular/core';
import { WeatherService } from './weather.service';

@Component({
  selector: 'app-weather',
  templateUrl: './weather.component.html'
})
export class WeatherComponent implements OnInit {
  forecasts: any[] = [];
  loading = false;
  error = '';

  constructor(private ws: WeatherService) {}

  ngOnInit(): void {
    this.loading = true;
    this.ws.get().subscribe({
      next: (data) => {
        this.forecasts = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = err?.message || err;
        this.loading = false;
      }
    });
  }
}
