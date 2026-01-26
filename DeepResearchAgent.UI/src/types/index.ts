// API Response Types
export interface ChatMessage {
  id: string
  role: 'user' | 'assistant' | 'system'
  content: string
  timestamp: Date
  metadata?: Record<string, unknown>
}

export interface ChatSession {
  id: string
  title: string
  messages: ChatMessage[]
  createdAt: Date
  updatedAt: Date
  config?: ResearchConfig
}

export interface ResearchConfig {
  language: string
  llmModels: string[]
  includedWebsites: string[]
  excludedWebsites: string[]
  topics: string[]
  maxDepth?: number
  timeoutSeconds?: number
}

export interface WebSearchTool {
  id: string
  name: string
  enabled: boolean
  type: 'web' | 'semantic' | 'local'
}

export interface ApiResponse<T> {
  success: boolean
  data?: T
  error?: string
  timestamp: Date
}
