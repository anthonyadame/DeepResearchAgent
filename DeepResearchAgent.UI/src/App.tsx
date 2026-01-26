import React from 'react'
import Sidebar from '@components/Sidebar'
import ChatDialog from '@components/ChatDialog'
import { apiService } from '@services/api'

function App() {
  const [currentSessionId, setCurrentSessionId] = React.useState<string | null>(null)

  const handleNewChat = async () => {
    try {
      const session = await apiService.createSession(`Chat ${new Date().toLocaleString()}`)
      setCurrentSessionId(session.id)
    } catch (err) {
      console.error('Failed to create session:', err)
    }
  }

  return (
    <div className="flex h-screen bg-gray-50">
      <Sidebar onNewChat={handleNewChat} />

      {/* Main Content */}
      <main className="flex-1 lg:ml-64 flex items-center justify-center p-4">
        {currentSessionId ? (
          <div className="w-full max-w-4xl h-full">
            <ChatDialog sessionId={currentSessionId} />
          </div>
        ) : (
          <div className="text-center">
            <h1 className="text-4xl font-bold text-gray-800 mb-4">Deep Research Agent</h1>
            <p className="text-gray-600 mb-8">Start a new conversation to begin your research</p>
            <button
              onClick={handleNewChat}
              className="px-6 py-3 bg-blue-500 text-white rounded-lg hover:bg-blue-600 transition-colors"
            >
              New Chat
            </button>
          </div>
        )}
      </main>
    </div>
  )
}

export default App
