import { useState } from 'react'
import { Menu, Plus, MessageSquare, X, History, ChevronLeft, Settings, Palette } from 'lucide-react'
import ChatHistoryPanel from './ChatHistoryPanel'
import ThemeDialog from './ThemeDialog'

interface SidebarProps {
  onNewChat: () => void
  currentSessionId: string | null
  onSelectSession: (sessionId: string) => void
}

type SidebarView = 'main' | 'history' | 'settings'

export default function Sidebar({ onNewChat, currentSessionId, onSelectSession }: SidebarProps) {
  const [isOpen, setIsOpen] = useState(true)
  const [currentView, setCurrentView] = useState<SidebarView>('main')
  const [showThemeDialog, setShowThemeDialog] = useState(false)

  const handleBackToMain = () => {
    setCurrentView('main')
  }

  return (
    <>
      {/* Toggle Button - Mobile */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="fixed top-4 left-4 z-50 p-2 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-lg lg:hidden bg-white dark:bg-gray-900 shadow-md"
      >
        {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
      </button>

      {/* Sidebar */}
      <aside
        className={`fixed left-0 top-0 h-screen w-64 bg-gray-900 text-white transform transition-transform duration-300 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        } lg:translate-x-0 z-40 flex flex-col`}
      >
        {/* Header */}
        <div className="p-4 border-b border-gray-700">
          {currentView !== 'main' ? (
            <button
              onClick={handleBackToMain}
              className="flex items-center gap-2 text-gray-300 hover:text-white transition-colors"
            >
              <ChevronLeft className="w-5 h-5" />
              <span>Back</span>
            </button>
          ) : (
            <h1 className="text-xl font-bold">Deep Research</h1>
          )}
        </div>

        {/* Content Area */}
        <div className="flex-1 overflow-hidden flex flex-col">
          {currentView === 'main' && (
            <>
              {/* New Chat Button */}
              <div className="p-4">
                <button
                  onClick={onNewChat}
                  className="w-full flex items-center gap-2 px-4 py-3 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
                >
                  <Plus className="w-5 h-5" />
                  <span>New Chat</span>
                </button>
              </div>

              {/* Navigation */}
              <nav className="flex-1 px-2 space-y-1">
                <SidebarItem 
                  icon={<History className="w-5 h-5" />} 
                  label="Chat History" 
                  onClick={() => setCurrentView('history')}
                />
                <SidebarItem 
                  icon={<Settings className="w-5 h-5" />} 
                  label="Settings" 
                  onClick={() => setCurrentView('settings')}
                />
                <SidebarItem 
                  icon={<Palette className="w-5 h-5" />} 
                  label="Themes" 
                  onClick={() => setShowThemeDialog(true)}
                />
              </nav>
            </>
          )}

          {currentView === 'history' && (
            <ChatHistoryPanel 
              currentSessionId={currentSessionId}
              onSelectSession={(id) => {
                onSelectSession(id)
                setCurrentView('main')
                setIsOpen(false) // Close on mobile after selection
              }}
            />
          )}

          {currentView === 'settings' && (
            <div className="p-4 text-gray-400">
              <p>Settings panel coming soon...</p>
            </div>
          )}
        </div>
      </aside>

      {/* Overlay - Mobile */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 lg:hidden z-30"
          onClick={() => setIsOpen(false)}
        />
      )}

      {/* Theme Dialog */}
      {showThemeDialog && (
        <ThemeDialog onClose={() => setShowThemeDialog(false)} />
      )}
    </>
  )
}

function SidebarItem({ icon, label, onClick }: { icon: React.ReactNode; label: string; onClick?: () => void }) {
  return (
    <button 
      onClick={onClick}
      className="w-full flex items-center gap-3 px-3 py-2 hover:bg-gray-800 rounded-lg transition-colors text-left"
    >
      <span className="text-gray-400">{icon}</span>
      <span className="text-sm">{label}</span>
    </button>
  )
}
