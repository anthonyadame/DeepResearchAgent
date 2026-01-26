import React from 'react'
import type { ChatMessage } from '@types/index'
import MessageBubble from './MessageBubble'

interface MessageListProps {
  messages: ChatMessage[]
  isLoading?: boolean
}

export default function MessageList({ messages, isLoading }: MessageListProps) {
  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-4">
      {messages.length === 0 ? (
        <div className="flex items-center justify-center h-full text-gray-400">
          <p>Start a new conversation</p>
        </div>
      ) : (
        messages.map((msg) => (
          <MessageBubble key={msg.id} message={msg} />
        ))
      )}
      {isLoading && (
        <div className="flex items-center gap-2 p-4 bg-blue-50 rounded-lg">
          <div className="flex gap-1">
            <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" />
            <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.1s' }} />
            <div className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0.2s' }} />
          </div>
          <span className="text-sm text-gray-600">Researching...</span>
        </div>
      )}
    </div>
  )
}
