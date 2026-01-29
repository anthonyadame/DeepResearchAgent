import axios, { AxiosInstance } from 'axios'
import type { ChatMessage, ChatSession, ResearchConfig } from '@types/index'

class ApiService {
  private client: AxiosInstance
  private baseURL: string

  constructor(baseURL: string = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api') {
    this.baseURL = baseURL
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

  /**
   * Stream a query to the chat endpoint with Server-Sent Events (SSE)
   * @param sessionId - The chat session ID
   * @param message - The user's message
   * @param config - Optional research configuration
   * @param onUpdate - Callback for each streaming update
   * @param onComplete - Callback when streaming completes
   * @param onError - Callback for errors
   * @returns AbortController to cancel the stream
   */
  streamQuery(
    sessionId: string,
    message: string,
    config: ResearchConfig | undefined,
    onUpdate: (update: string) => void,
    onComplete: () => void,
    onError: (error: Error) => void
  ): AbortController {
    const abortController = new AbortController()
    const url = `${this.baseURL}/chat/${sessionId}/stream`

    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ message, config }),
      signal: abortController.signal,
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`)
        }

        const reader = response.body?.getReader()
        if (!reader) {
          throw new Error('Response body is not readable')
        }

        const decoder = new TextDecoder()

        try {
          while (true) {
            const { done, value } = await reader.read()
            
            if (done) {
              onComplete()
              break
            }

            const chunk = decoder.decode(value)
            const lines = chunk.split('\n')

            for (const line of lines) {
              if (line.startsWith('data: ')) {
                const data = line.substring(6).trim()
                
                if (data === '[DONE]') {
                  onComplete()
                  return
                } else if (data === '[CANCELLED]') {
                  onError(new Error('Stream was cancelled'))
                  return
                } else if (data.startsWith('{') && data.includes('error')) {
                  try {
                    const errorObj = JSON.parse(data)
                    onError(new Error(errorObj.error))
                  } catch {
                    onUpdate(data)
                  }
                } else if (data) {
                  onUpdate(data)
                }
              }
            }
          }
        } catch (error) {
          if (error instanceof Error && error.name === 'AbortError') {
            console.log('Stream aborted by user')
          } else {
            onError(error instanceof Error ? error : new Error('Stream error'))
          }
        } finally {
          reader.releaseLock()
        }
      })
      .catch((error) => {
        if (error.name === 'AbortError') {
          console.log('Fetch aborted')
        } else {
          onError(error)
        }
      })

    return abortController
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
