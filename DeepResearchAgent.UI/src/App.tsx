import { useState, useEffect } from 'react'
import Sidebar from '@components/Sidebar'
import ChatDialog from '@components/ChatDialog'
import { ThemeProvider } from '@contexts/ThemeContext'
import { apiService } from '@services/api'

function App() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)

  // Initialize session on mount
  useEffect(() => {
    const initializeSession = async () => {
      try {
        // Try to load last session from localStorage
        const lastSessionId = localStorage.getItem('lastSessionId')
        
        if (lastSessionId) {
          // Verify session still exists on server
          try {
            await apiService.getChatHistory(lastSessionId)
            setCurrentSessionId(lastSessionId)
            setIsInitializing(false)
            return
          } catch (err) {
            // Session no longer exists, remove from localStorage
            localStorage.removeItem('lastSessionId')
          }
        }
        
        // Create new session if no valid session exists
        const session = await apiService.createSession(`New Chat ${new Date().toLocaleTimeString()}`)
        setCurrentSessionId(session.id)
      } catch (err) {
        console.error('Failed to initialize session:', err)
        // Create a temporary offline session ID
        setCurrentSessionId('offline-' + Date.now())
      } finally {
        setIsInitializing(false)
      }
    }

    initializeSession()
  }, [])

  // Save current session to localStorage
  useEffect(() => {
    if (currentSessionId && !currentSessionId.startsWith('offline-')) {
      localStorage.setItem('lastSessionId', currentSessionId)
    }
  }, [currentSessionId])

  const handleNewChat = async () => {
    try {
      const session = await apiService.createSession(`New Chat ${new Date().toLocaleTimeString()}`)
      setCurrentSessionId(session.id)
    } catch (err) {
      console.error('Failed to create session:', err)
      // Create offline session
      setCurrentSessionId('offline-' + Date.now())
    }
  }

  const handleSelectSession = (sessionId: string) => {
    setCurrentSessionId(sessionId)
  }

  return (
    <ThemeProvider>
      <div className="flex h-screen bg-gray-50 dark:bg-gray-900">
        <Sidebar 
          onNewChat={handleNewChat} 
          currentSessionId={currentSessionId}
          onSelectSession={handleSelectSession}
        />

        {/* Main Content */}
        <main className="flex-1 lg:ml-64 flex items-center justify-center p-4">
          {isInitializing ? (
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-600 dark:text-gray-400">Initializing...</p>
            </div>
          ) : currentSessionId ? (
            <div className="w-full max-w-5xl h-full">
              <ChatDialog sessionId={currentSessionId} />
            </div>
          ) : (
            <div className="text-center">
              <h1 className="text-4xl font-bold text-gray-800 dark:text-white mb-4">
                Deep Research Agent
              </h1>
              <p className="text-gray-600 dark:text-gray-400 mb-8">
                Unable to connect to backend. Working in offline mode.
              </p>
              <button
                onClick={handleNewChat}
                className="px-6 py-3 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors"
              >
                Try Again
              </button>
            </div>
          )}
        </main>
      </div>
    </ThemeProvider>
  )
}

export default App
