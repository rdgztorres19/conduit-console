import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';

export interface TagReadResponse {
  tagName: string;
  value: any;
  quality: string;
  timestamp: string;
  correlationId?: string;
  hasError: boolean;
  errorMessage?: string;
}

export interface TagWriteResponse {
  tagName: string;
  path: string;
  fullTagName: string;
  value: any;
  success: boolean;
  timestamp: string;
  correlationId?: string;
  errorMessage?: string;
}

type WebSocketState = 'connecting' | 'connected' | 'disconnected' | 'error';

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private websocket?: WebSocket;
  private connectionState$ = new Subject<WebSocketState>();
  private tagReadResponse$ = new Subject<TagReadResponse>();
  private tagWriteResponse$ = new Subject<TagWriteResponse>();
  private reconnectAttempts = 0;
  private maxReconnectAttempts = 5;
  private reconnectDelay = 1000;
  private reconnectTimer?: number;

  // Observables públicos
  public connectionState = this.connectionState$.asObservable();
  public tagReadResponse = this.tagReadResponse$.asObservable();
  public tagWriteResponse = this.tagWriteResponse$.asObservable();

  constructor() {
    this.startConnection();
  }

  private startConnection() {
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws/plctag`;
    
    console.log(`🔌 Connecting to WebSocket: ${wsUrl}`);
    this.connectionState$.next('connecting');

    try {
      this.websocket = new WebSocket(wsUrl);

      this.websocket.onopen = () => {
        console.log('✅ WebSocket connected');
        this.connectionState$.next('connected');
        this.reconnectAttempts = 0;
        
        // Enviar ping periódico para mantener la conexión viva
        this.startPingInterval();
      };

      this.websocket.onmessage = (event) => {
        try {
          const message = JSON.parse(event.data);
          this.handleMessage(message);
        } catch (err) {
          console.error('❌ Error parsing WebSocket message:', err);
        }
      };

      this.websocket.onerror = (error) => {
        console.error('❌ WebSocket error:', error);
        this.connectionState$.next('error');
      };

      this.websocket.onclose = () => {
        console.log('🔴 WebSocket disconnected');
        this.connectionState$.next('disconnected');
        this.stopPingInterval();
        this.attemptReconnect();
      };
    } catch (err) {
      console.error('❌ Error creating WebSocket:', err);
      this.connectionState$.next('error');
      this.attemptReconnect();
    }
  }

  private handleMessage(message: any) {
    console.log('📥 WebSocket message received:', message);
    
    if (message.type === 'TagReadResponse') {
      const tagResponse: TagReadResponse = {
        tagName: message.tagName || '',
        value: message.value,
        quality: message.quality || '',
        timestamp: message.timestamp || new Date().toISOString(),
        correlationId: message.correlationId,
        hasError: message.hasError || false,
        errorMessage: message.errorMessage
      };
      console.log('   Converted TagReadResponse:', tagResponse);
      this.tagReadResponse$.next(tagResponse);
    } else if (message.type === 'TagWriteResponse') {
      const writeResponse: TagWriteResponse = {
        tagName: message.tagName || '',
        path: message.path || '',
        fullTagName: message.fullTagName || '',
        value: message.value,
        success: message.success || false,
        timestamp: message.timestamp || new Date().toISOString(),
        correlationId: message.correlationId,
        errorMessage: message.errorMessage
      };
      this.tagWriteResponse$.next(writeResponse);
    } else if (message.type === 'pong') {
      // Respuesta al ping, conexión está viva
      console.log('🏓 Received pong');
    }
  }

  private attemptReconnect() {
    if (this.reconnectAttempts >= this.maxReconnectAttempts) {
      console.error(`❌ Max reconnect attempts (${this.maxReconnectAttempts}) reached`);
      return;
    }

    this.reconnectAttempts++;
    const delay = this.reconnectDelay * this.reconnectAttempts;
    
    console.log(`🔄 Attempting to reconnect in ${delay}ms (attempt ${this.reconnectAttempts}/${this.maxReconnectAttempts})`);
    
    this.reconnectTimer = window.setTimeout(() => {
      this.startConnection();
    }, delay);
  }

  private pingInterval?: number;
  
  private startPingInterval() {
    this.pingInterval = window.setInterval(() => {
      if (this.websocket?.readyState === WebSocket.OPEN) {
        this.sendMessage({ type: 'ping' });
      }
    }, 30000); // Ping cada 30 segundos
  }

  private stopPingInterval() {
    if (this.pingInterval) {
      clearInterval(this.pingInterval);
      this.pingInterval = undefined;
    }
  }

  subscribeToTag(tagName: string): void {
    if (this.websocket?.readyState === WebSocket.OPEN) {
      this.sendMessage({
        type: 'subscribe',
        tagName: tagName
      });
      console.log(`✅ Subscribed to tag: ${tagName}`);
    } else {
      console.warn(`⚠️ Cannot subscribe to tag ${tagName}: WebSocket not connected`);
    }
  }

  unsubscribeFromTag(tagName: string): void {
    if (this.websocket?.readyState === WebSocket.OPEN) {
      this.sendMessage({
        type: 'unsubscribe',
        tagName: tagName
      });
      console.log(`❌ Unsubscribed from tag: ${tagName}`);
    }
  }

  private sendMessage(message: any): void {
    if (this.websocket?.readyState === WebSocket.OPEN) {
      this.websocket.send(JSON.stringify(message));
    } else {
      console.warn('⚠️ Cannot send message: WebSocket not connected');
    }
  }

  getConnectionState(): WebSocketState {
    if (!this.websocket) return 'disconnected';
    
    switch (this.websocket.readyState) {
      case WebSocket.CONNECTING:
        return 'connecting';
      case WebSocket.OPEN:
        return 'connected';
      case WebSocket.CLOSING:
      case WebSocket.CLOSED:
        return 'disconnected';
      default:
        return 'error';
    }
  }

  isConnected(): boolean {
    return this.websocket?.readyState === WebSocket.OPEN;
  }

  disconnect(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = undefined;
    }
    this.stopPingInterval();
    if (this.websocket) {
      this.websocket.close();
      this.websocket = undefined;
    }
  }
}

