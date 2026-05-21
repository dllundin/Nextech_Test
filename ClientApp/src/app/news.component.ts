import { Component, OnInit } from '@angular/core';
import { NewsService, HackerNewsStory } from './news.service';

@Component({
  selector: 'app-news',
  templateUrl: './news.component.html',
  styleUrls: ['./news.component.css']
})
export class NewsComponent implements OnInit {
  stories: HackerNewsStory[] = [];
  filteredStories: HackerNewsStory[] = [];
  loading = false;
  error: string | null = null;
  selectedCount = 10;
  searchQuery = '';
  searchType: 'all' | 'id' | 'by' | 'title' | 'date' = 'all';

  constructor(private newsService: NewsService) {}

  ngOnInit(): void {
    this.loadTopStories();
  }

  loadTopStories(): void {
    this.loading = true;
    this.error = null;
    this.searchQuery = '';
    this.newsService.getTopStories(this.selectedCount).subscribe({
      next: (data) => {
        this.stories = data;
        this.filteredStories = data;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load stories. Please try again.';
        this.loading = false;
        console.error('Error fetching stories:', err);
      }
    });
  }

  onCountChange(count: number): void {
    this.selectedCount = count;
    this.loadTopStories();
  }

  onSearch(): void {
    if (!this.searchQuery.trim()) {
      this.filteredStories = this.stories;
      return;
    }

    const query = this.searchQuery.toLowerCase().trim();
    this.filteredStories = this.stories.filter(story => this.matchesSearch(story, query));
  }

  private matchesSearch(story: HackerNewsStory, query: string): boolean {
    switch (this.searchType) {
      case 'id':
        return story.id.toString().includes(query);
      case 'by':
        return (story.by?.toLowerCase() || '').includes(query);
      case 'title':
        return (story.title?.toLowerCase() || '').includes(query);
      case 'date':
        return this.formatTime(story.time).toLowerCase().includes(query);
      case 'all':
      default:
        return (
          story.id.toString().includes(query) ||
          (story.by?.toLowerCase() || '').includes(query) ||
          (story.title?.toLowerCase() || '').includes(query) ||
          this.formatTime(story.time).toLowerCase().includes(query)
        );
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.filteredStories = this.stories;
  }

  openStory(url: string | undefined): void {
    if (url) {
      window.open(url, '_blank');
    }
  }

  formatTime(timestamp: number): string {
    return new Date(timestamp * 1000).toLocaleString();
  }

  getResultCount(): number {
    return this.filteredStories.length;
  }
}
