import { useState, useCallback } from 'react'
import type { ChatMessage, ResearchConfig } from '@types/index'
import { apiService } from '@services/api'

export const useChat = (sessionId: string) => {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const loadHistory = useCallback(async () => {
    try {
      setIsLoading(true)
      const history = await apiService.getChatHistory(sessionId)
      setMessages(history)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load chat history')
    } finally {
      setIsLoading(false)
    }
  }, [sessionId])

  const sendMessage = useCallback(async (content: string, config?: ResearchConfig) => {
    try {
      setIsLoading(true)
      setError(null)
      const response = await apiService.submitQuery(sessionId, content, config)
      setMessages(prev => [...prev, response])
      return response
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to send message'
      setError(errorMsg)
      throw err
    } finally {
      setIsLoading(false)
    }
  }, [sessionId])

  return { messages, isLoading, error, loadHistory, sendMessage }
}
