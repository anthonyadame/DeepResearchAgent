import { useState, useCallback, useRef } from 'react'
import type { ChatMessage, ResearchConfig } from '@types/index'
import { apiService } from '@services/api'

export const useChat = (sessionId: string) => {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [streamingMessage, setStreamingMessage] = useState<string>('')
  const [isStreaming, setIsStreaming] = useState(false)
  const abortControllerRef = useRef<AbortController | null>(null)

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
      
      // Add user message immediately
      const userMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content,
        timestamp: new Date().toISOString(),
        metadata: null
      }
      setMessages(prev => [...prev, userMessage])

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

  const sendMessageStreaming = useCallback(async (content: string, config?: ResearchConfig) => {
    try {
      setIsStreaming(true)
      setError(null)
      setStreamingMessage('')

      // Add user message immediately
      const userMessage: ChatMessage = {
        id: crypto.randomUUID(),
        role: 'user',
        content,
        timestamp: new Date().toISOString(),
        metadata: null
      }
      setMessages(prev => [...prev, userMessage])

      // Start streaming
      const controller = apiService.streamQuery(
        sessionId,
        content,
        config,
        // onUpdate callback
        (update: string) => {
          setStreamingMessage(prev => prev + update + '\n')
        },
        // onComplete callback
        () => {
          // Save the complete message to history
          const assistantMessage: ChatMessage = {
            id: crypto.randomUUID(),
            role: 'assistant',
            content: streamingMessage,
            timestamp: new Date().toISOString(),
            metadata: { streamed: true, config }
          }
          setMessages(prev => [...prev, assistantMessage])
          setStreamingMessage('')
          setIsStreaming(false)
          abortControllerRef.current = null
        },
        // onError callback
        (error: Error) => {
          setError(error.message)
          setIsStreaming(false)
          setStreamingMessage('')
          abortControllerRef.current = null
        }
      )

      abortControllerRef.current = controller
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to send streaming message'
      setError(errorMsg)
      setIsStreaming(false)
      setStreamingMessage('')
      throw err
    }
  }, [sessionId, streamingMessage])

  const cancelStreaming = useCallback(() => {
    if (abortControllerRef.current) {
      abortControllerRef.current.abort()
      abortControllerRef.current = null
      setIsStreaming(false)
      setStreamingMessage('')
    }
  }, [])

  return { 
    messages, 
    isLoading, 
    error, 
    loadHistory, 
    sendMessage,
    sendMessageStreaming,
    isStreaming,
    streamingMessage,
    cancelStreaming
  }
}
