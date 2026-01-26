import axios, { AxiosInstance } from 'axios'
import type { ChatMessage, ChatSession, ResearchConfig } from '@types/index'

class ApiService {
  private client: AxiosInstance

  constructor(baseURL: string = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api') {
    this.client = axios.create({
      baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json',
      },
    })

    // Response interceptor for error handling
    this.client.interceptors.response.use(
      response => response,
      error => {
        console.error('API Error:', error.response?.data || error.message)
        return Promise.reject(error)
      }
    )
  }

  // Chat endpoints
  async submitQuery(sessionId: string, message: string, config?: ResearchConfig): Promise<ChatMessage> {
    const response = await this.client.post(`/chat/${sessionId}/query`, {
      message,
      config,
    })
    return response.data
  }

  async getChatHistory(sessionId: string): Promise<ChatMessage[]> {
    const response = await this.client.get(`/chat/${sessionId}/history`)
    return response.data
  }

  async createSession(title?: string): Promise<ChatSession> {
    const response = await this.client.post('/chat/sessions', { title })
    return response.data
  }

  async getSessions(): Promise<ChatSession[]> {
    const response = await this.client.get('/chat/sessions')
    return response.data
  }

  async deleteSession(sessionId: string): Promise<void> {
    await this.client.delete(`/chat/sessions/${sessionId}`)
  }

  // File upload
  async uploadFile(sessionId: string, file: File): Promise<{ id: string; name: string }> {
    const formData = new FormData()
    formData.append('file', file)
    const response = await this.client.post(`/chat/${sessionId}/files`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    })
    return response.data
  }

  // Configuration
  async getAvailableModels(): Promise<string[]> {
    const response = await this.client.get('/config/models')
    return response.data
  }

  async getSearchTools(): Promise<{ id: string; name: string }[]> {
    const response = await this.client.get('/config/search-tools')
    return response.data
  }

  async saveConfig(config: ResearchConfig): Promise<void> {
    await this.client.post('/config/save', config)
  }
}

export const apiService = new ApiService()
