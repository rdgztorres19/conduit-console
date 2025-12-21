import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AvailableTag {
  name: string;
  type: string;
}

export interface TagReadRequest {
  tagName: string;
  correlationId?: string;
}

export interface TagWriteRequest {
  tagName: string;
  path: string;
  value: any;
  correlationId?: string;
}

@Injectable({
  providedIn: 'root'
})
export class MqttService {
  constructor(private http: HttpClient) {}

  getAvailableTags(): Observable<{ tags: AvailableTag[] }> {
    return this.http.get<{ tags: AvailableTag[] }>('/api/mqtt/tags/available');
  }

  sendTagReadRequest(request: TagReadRequest): Observable<any> {
    return this.http.post('/api/mqtt/tags/read-request', request);
  }

  sendTagWriteRequest(request: TagWriteRequest): Observable<any> {
    return this.http.post('/api/mqtt/tags/write-request', request);
  }
}

