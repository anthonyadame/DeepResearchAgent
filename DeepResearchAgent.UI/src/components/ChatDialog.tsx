import React, { useEffect, useState } from 'react'
import { Send, Plus, Link as LinkIcon, Settings, Search } from 'lucide-react'
import type { ChatMessage, ResearchConfig } from '@types/index'
import { useChat } from '@hooks/useChat'
import InputBar from './InputBar'
import MessageList from './MessageList'
import FileUploadModal from './FileUploadModal'
import ConfigurationDialog from './ConfigurationDialog'

interface ChatDialogProps {
  sessionId: string
}

export default function ChatDialog({ sessionId }: ChatDialogProps) {
  const { messages, isLoading, sendMessage, loadHistory } = useChat(sessionId)
  const [input, setInput] = useState('')
  const [showFileModal, setShowFileModal] = useState(false)
  const [showConfigDialog, setShowConfigDialog] = useState(false)
  const [config, setConfig] = useState<ResearchConfig | undefined>()

  useEffect(() => {
    loadHistory()
  }, [sessionId, loadHistory])

  const handleSendMessage = async () => {
    if (!input.trim()) return

    try {
      await sendMessage(input, config)
      setInput('')
    } catch (err) {
      console.error('Error sending message:', err)
    }
  }

  return (
    <div className="flex flex-col h-full bg-white rounded-lg shadow-lg">
      {/* Header */}
      <div className="border-b border-gray-200 p-4">
        <h2 className="text-xl font-semibold text-gray-800">Research Chat</h2>
      </div>

      {/* Messages */}
      <MessageList messages={messages} isLoading={isLoading} />

      {/* Input Bar */}
      <div className="border-t border-gray-200 p-4 space-y-3">
        <div className="flex gap-2">
          <button
            onClick={() => setShowFileModal(true)}
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            title="Add items"
          >
            <Plus className="w-5 h-5 text-gray-600" />
          </button>

          <button
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            title="Web search tools"
          >
            <Search className="w-5 h-5 text-gray-600" />
          </button>

          <button
            onClick={() => setShowConfigDialog(true)}
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            title="Configuration"
          >
            <Settings className="w-5 h-5 text-gray-600" />
          </button>

          <button
            className="p-2 hover:bg-gray-100 rounded-lg transition-colors"
            title="Attach webpage"
          >
            <LinkIcon className="w-5 h-5 text-gray-600" />
          </button>
        </div>

        <InputBar
          value={input}
          onChange={setInput}
          onSend={handleSendMessage}
          isLoading={isLoading}
        />
      </div>

      {/* Modals */}
      {showFileModal && (
        <FileUploadModal
          sessionId={sessionId}
          onClose={() => setShowFileModal(false)}
        />
      )}
      {showConfigDialog && (
        <ConfigurationDialog
          config={config}
          onSave={(newConfig) => {
            setConfig(newConfig)
            setShowConfigDialog(false)
          }}
          onClose={() => setShowConfigDialog(false)}
        />
      )}
    </div>
  )
}
