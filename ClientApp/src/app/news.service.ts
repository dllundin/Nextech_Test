import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface HackerNewsStory {
  id: number;
  by: string;
  title: string;
  url: string;
  score: number;
  time: number;
  descendants: number;
  type: string;
  kids: number[];
}

@Injectable({
  providedIn: 'root'
})
export class NewsService {
  private apiUrl = '/api/news';

  constructor(private http: HttpClient) {}

  getTopStories(count: number = 10): Observable<HackerNewsStory[]> {
    console.log(this.apiUrl, );
    return this.http.get<HackerNewsStory[]>(`${this.apiUrl}/top?count=${count}`);
  }

  getStory(id: number): Observable<HackerNewsStory> {
    return this.http.get<HackerNewsStory>(`${this.apiUrl}/${id}`);
  }
}
