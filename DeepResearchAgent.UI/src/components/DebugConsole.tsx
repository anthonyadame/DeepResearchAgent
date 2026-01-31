import React, { useMemo } from 'react'
import { X, Trash2 } from 'lucide-react'
import { useDebugStore } from '@stores/debugStore'
import DebugMessageItem from './DebugMessageItem'

interface DebugConsoleProps {
  isVisible: boolean
  height: number
}

export default function DebugConsole({ isVisible, height }: DebugConsoleProps) {
  const { messages, activeTab, setActiveTab, clearMessages } = useDebugStore()

  const filteredMessages = useMemo(() => {
    switch (activeTab) {
      case 'messages':
        return messages.filter((m) => m.type === 'message')
      case 'state':
        return messages.filter((m) => m.type === 'state')
      case 'api_calls':
        return messages.filter((m) => m.type === 'api_call' || m.type === 'error')
      default:
        return messages
    }
  }, [messages, activeTab])

  if (!isVisible) return null

  return (
    <div
      className="flex flex-col bg-gray-50 border-t border-gray-200"
      style={{ height: `${height}%` }}
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-2 bg-gray-800 text-white">
        <div className="flex items-center gap-4">
          <h3 className="text-sm font-semibold">Debug Console</h3>
          <div className="flex items-center gap-1 bg-gray-700 rounded-md p-0.5">
            <TabButton
              active={activeTab === 'messages'}
              onClick={() => setActiveTab('messages')}
              label="Messages"
              count={messages.filter((m) => m.type === 'message').length}
            />
            <TabButton
              active={activeTab === 'state'}
              onClick={() => setActiveTab('state')}
              label="State"
              count={messages.filter((m) => m.type === 'state').length}
            />
            <TabButton
              active={activeTab === 'api_calls'}
              onClick={() => setActiveTab('api_calls')}
              label="API Calls"
              count={
                messages.filter((m) => m.type === 'api_call' || m.type === 'error').length
              }
            />
          </div>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={clearMessages}
            className="p-1.5 hover:bg-gray-700 rounded transition-colors"
            title="Clear console"
          >
            <Trash2 className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-y-auto bg-white">
        {filteredMessages.length === 0 ? (
          <div className="flex items-center justify-center h-full text-gray-400">
            <div className="text-center">
              <p className="text-sm">No {activeTab.replace('_', ' ')} logged yet</p>
              <p className="text-xs mt-1">
                Debug information will appear here as you interact with the chat
              </p>
            </div>
          </div>
        ) : (
          <div className="divide-y divide-gray-200">
            {filteredMessages.map((message) => (
              <DebugMessageItem key={message.id} message={message} />
            ))}
          </div>
        )}
      </div>

      {/* Footer with message count */}
      <div className="px-4 py-1.5 bg-gray-100 border-t border-gray-200">
        <p className="text-xs text-gray-600">
          {filteredMessages.length} {activeTab.replace('_', ' ')} •{' '}
          {messages.length} total debug entries
        </p>
      </div>
    </div>
  )
}

interface TabButtonProps {
  active: boolean
  onClick: () => void
  label: string
  count: number
}

function TabButton({ active, onClick, label, count }: TabButtonProps) {
  return (
    <button
      onClick={onClick}
      className={`px-3 py-1 text-xs font-medium rounded transition-colors ${
        active
          ? 'bg-gray-900 text-white'
          : 'text-gray-300 hover:text-white hover:bg-gray-600'
      }`}
    >
      {label}
      {count > 0 && (
        <span
          className={`ml-1.5 px-1.5 py-0.5 rounded-full text-[10px] ${
            active ? 'bg-blue-500 text-white' : 'bg-gray-600 text-gray-300'
          }`}
        >
          {count}
        </span>
      )}
    </button>
  )
}
