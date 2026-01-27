import React, { useEffect, useState } from 'react'
import { Send, Plus, Settings, Globe, ChevronDown, Paperclip, Database } from 'lucide-react'
import type { ChatMessage, ResearchConfig } from '@types/index'
import { useChat } from '@hooks/useChat'
import InputBar from './InputBar'
import MessageList from './MessageList'
import FileUploadModal from './FileUploadModal'
import ConfigurationDialog from './ConfigurationDialog'
import WebSearchDialog from './WebSearchDialog'
import DropdownMenu from './DropdownMenu'

interface ChatDialogProps {
  sessionId: string
}

export default function ChatDialog({ sessionId }: ChatDialogProps) {
  const { messages, isLoading, sendMessage, loadHistory } = useChat(sessionId)
  const [input, setInput] = useState('')
  const [showFileModal, setShowFileModal] = useState(false)
  const [showConfigDialog, setShowConfigDialog] = useState(false)
  const [showWebSearchDialog, setShowWebSearchDialog] = useState(false)
  const [showAddMenu, setShowAddMenu] = useState(false)
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

  const addMenuItems = [
    {
      icon: <Paperclip className="w-4 h-4" />,
      label: 'Upload Files',
      onClick: () => {
        setShowFileModal(true)
        setShowAddMenu(false)
      }
    },
    {
      icon: <Globe className="w-4 h-4" />,
      label: 'Attach Webpage',
      onClick: () => {
        // TODO: Implement webpage attachment
        setShowAddMenu(false)
      }
    },
    {
      icon: <Database className="w-4 h-4" />,
      label: 'Attach Knowledge',
      onClick: () => {
        // TODO: Implement knowledge base attachment (Qdrant, etc.)
        setShowAddMenu(false)
      }
    }
  ]

  return (
    <div className="flex flex-col h-full bg-gradient-to-b from-gray-50 to-white">
      {/* Header - Minimal like ChatGPT */}
      <div className="border-b border-gray-100 px-4 py-3 flex items-center justify-center">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-gray-400">○</span>
          <h2 className="text-lg font-semibold text-gray-700">Deep Research Agent</h2>
        </div>
      </div>

      {/* Messages */}
      <div className="flex-1 overflow-hidden">
        <MessageList messages={messages} isLoading={isLoading} />
      </div>

      {/* Action Bar - Bottom Section */}
      <div className="border-t border-gray-100 p-4 bg-white">
        <div className="max-w-4xl mx-auto">
          {/* Input Container */}
          <div className="relative bg-white border border-gray-200 rounded-2xl shadow-sm hover:shadow-md transition-shadow">
            {/* Input Field */}
            <div className="px-4 py-3">
              <InputBar
                value={input}
                onChange={setInput}
                onSend={handleSendMessage}
                isLoading={isLoading}
              />
            </div>

            {/* Button Row */}
            <div className="flex items-center justify-between px-3 pb-3 pt-1">
              {/* Left Group - Action Buttons */}
              <div className="flex items-center gap-1">
                {/* Add Items Button with Dropdown */}
                <div className="relative">
                  <button
                    onClick={() => setShowAddMenu(!showAddMenu)}
                    className="p-2 hover:bg-gray-100 rounded-lg transition-colors flex items-center gap-1 group"
                    title="Add items"
                    disabled={isLoading}
                  >
                    <Plus className="w-4 h-4 text-gray-600 group-hover:text-gray-800" />
                    <ChevronDown className="w-3 h-3 text-gray-400" />
                  </button>
                  
                  {showAddMenu && (
                    <DropdownMenu
                      items={addMenuItems}
                      onClose={() => setShowAddMenu(false)}
                    />
                  )}
                </div>

                {/* Web Search Tools Button */}
                <button
                  onClick={() => setShowWebSearchDialog(true)}
                  className="p-2 hover:bg-gray-100 rounded-lg transition-colors group"
                  title="Web search tools"
                  disabled={isLoading}
                >
                  <Globe className="w-4 h-4 text-gray-600 group-hover:text-gray-800" />
                </button>

                {/* Configuration Button */}
                <button
                  onClick={() => setShowConfigDialog(true)}
                  className="p-2 hover:bg-gray-100 rounded-lg transition-colors group"
                  title="Configuration"
                  disabled={isLoading}
                >
                  <Settings className="w-4 h-4 text-gray-600 group-hover:text-gray-800" />
                </button>
              </div>

              {/* Right Group - Submit Button */}
              <button
                onClick={handleSendMessage}
                disabled={isLoading || !input.trim()}
                className="px-4 py-2 bg-gray-900 hover:bg-gray-800 disabled:bg-gray-300 text-white rounded-lg transition-all flex items-center gap-2 font-medium text-sm disabled:cursor-not-allowed"
                title="Send message"
              >
                <Send className="w-4 h-4" />
                <span>Send</span>
              </button>
            </div>
          </div>

          {/* Helper Text */}
          <p className="text-xs text-gray-400 text-center mt-3">
            Deep Research Agent can make mistakes. Verify important information.
          </p>
        </div>
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
      {showWebSearchDialog && (
        <WebSearchDialog
          onClose={() => setShowWebSearchDialog(false)}
          onSelect={(provider) => {
            // TODO: Handle web search provider selection
            console.log('Selected provider:', provider)
            setShowWebSearchDialog(false)
          }}
        />
      )}
    </div>
  )
}
