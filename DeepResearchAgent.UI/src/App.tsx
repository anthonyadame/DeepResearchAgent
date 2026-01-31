import { useState, useEffect } from 'react'
import Sidebar from '@components/Sidebar'
import ChatDialog from '@components/ChatDialog'
import ResearchStreamingPanel from '@components/ResearchStreamingPanel'
import { ThemeProvider } from '@contexts/ThemeContext'
import { apiService } from '@services/api'

function App() {
  const [currentSessionId, setCurrentSessionId] = useState<string | null>(null)
  const [isInitializing, setIsInitializing] = useState(true)
  const [viewMode, setViewMode] = useState<'chat' | 'research'>('chat')
  const [debugError, setDebugError] = useState<string>('')

  // Initialize session on mount
  useEffect(() => {
    const initializeSession = async () => {
      try {
        console.log('[App] Initializing session...')
        
        // Try to load last session from localStorage
        const lastSessionId = localStorage.getItem('lastSessionId')
        console.log('[App] Last session ID:', lastSessionId)
        
        if (lastSessionId) {
          // Verify session still exists on server
          try {
            console.log('[App] Verifying last session:', lastSessionId)
            await apiService.getChatHistory(lastSessionId)
            console.log('[App] Session verified, setting:', lastSessionId)
            setCurrentSessionId(lastSessionId)
            setIsInitializing(false)
            return
          } catch (err) {
            console.warn('[App] Last session not available:', err)
            localStorage.removeItem('lastSessionId')
          }
        }
        
        // Create new session if no valid session exists
        console.log('[App] Creating new session...')
        const session = await apiService.createSession(`New Chat ${new Date().toLocaleTimeString()}`)
        console.log('[App] Session created:', session)
        setCurrentSessionId(session.id)
        setDebugError('')
      } catch (err) {
        console.error('[App] Failed to initialize session:', err)
        const errorMsg = err instanceof Error ? err.message : String(err)
        setDebugError(errorMsg)
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
        <main className="flex-1 lg:ml-64 flex flex-col items-center justify-center p-4">
          {isInitializing ? (
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-600 dark:text-gray-400">Initializing...</p>
            </div>
          ) : currentSessionId ? (
            <div className="w-full max-w-5xl h-full flex flex-col">
              {/* View Mode Selector */}
              <div className="flex gap-2 mb-4 border-b border-gray-200 dark:border-gray-700">
                <button
                  onClick={() => setViewMode('research')}
                  className={`px-4 py-2 font-medium transition-colors ${
                    viewMode === 'research'
                      ? 'text-blue-600 border-b-2 border-blue-600'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                  }`}
                >
                  🔍 Research
                </button>
                <button
                  onClick={() => setViewMode('chat')}
                  className={`px-4 py-2 font-medium transition-colors ${
                    viewMode === 'chat'
                      ? 'text-blue-600 border-b-2 border-blue-600'
                      : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200'
                  }`}
                >
                  💬 Chat
                </button>
              </div>

              {/* Content */}
              <div className="flex-1 overflow-hidden">
                {viewMode === 'research' ? (
                  <ResearchStreamingPanel />
                ) : (
                  <ChatDialog sessionId={currentSessionId} />
                )}
              </div>
            </div>
          ) : (
            <div className="text-center">
              <h1 className="text-4xl font-bold text-gray-800 dark:text-white mb-4">
                Deep Research Agent
              </h1>
              <p className="text-gray-600 dark:text-gray-400 mb-8">
                Unable to connect to backend. Working in offline mode.
              </p>
              {debugError && (
                <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
                  <p className="font-bold">Debug Error:</p>
                  <p className="text-sm">{debugError}</p>
                </div>
              )}
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
