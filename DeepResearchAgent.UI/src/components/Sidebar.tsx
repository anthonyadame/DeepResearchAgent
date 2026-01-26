import React, { useState } from 'react'
import { Menu, Plus, MessageSquare, Settings, Palette, X } from 'lucide-react'

interface SidebarProps {
  onNewChat: () => void
}

export default function Sidebar({ onNewChat }: SidebarProps) {
  const [isOpen, setIsOpen] = useState(true)

  return (
    <>
      {/* Toggle Button */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="fixed top-4 left-4 z-50 p-2 hover:bg-gray-100 rounded-lg lg:hidden"
      >
        {isOpen ? <X className="w-6 h-6" /> : <Menu className="w-6 h-6" />}
      </button>

      {/* Sidebar */}
      <aside
        className={`fixed left-0 top-0 h-screen w-64 bg-gray-900 text-white transform transition-transform duration-300 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        } lg:translate-x-0 z-40`}
      >
        <div className="p-4 space-y-4">
          {/* Logo */}
          <div className="py-4 border-b border-gray-700">
            <h1 className="text-xl font-bold">Deep Research</h1>
          </div>

          {/* New Chat Button */}
          <button
            onClick={onNewChat}
            className="w-full flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg transition-colors"
          >
            <Plus className="w-5 h-5" />
            <span>New Chat</span>
          </button>

          {/* Search */}
          <input
            type="text"
            placeholder="Search chats..."
            className="w-full px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
          />

          {/* Navigation */}
          <nav className="space-y-2">
            <div>
              <h3 className="text-xs uppercase text-gray-400 font-semibold px-4 py-2">
                Chat Histories
              </h3>
              <div className="space-y-1">
                {/* Chat history items will be rendered here */}
                <p className="text-gray-400 text-sm px-4 py-2">No chats yet</p>
              </div>
            </div>
          </nav>

          {/* Settings Section */}
          <div className="absolute bottom-0 left-0 right-0 border-t border-gray-700 p-4 space-y-2">
            <button className="w-full flex items-center gap-2 px-4 py-2 hover:bg-gray-800 rounded-lg transition-colors">
              <Settings className="w-5 h-5" />
              <span>Configurations</span>
            </button>
            <button className="w-full flex items-center gap-2 px-4 py-2 hover:bg-gray-800 rounded-lg transition-colors">
              <Palette className="w-5 h-5" />
              <span>Theme</span>
            </button>
          </div>
        </div>
      </aside>

      {/* Overlay */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black bg-opacity-50 lg:hidden z-30"
          onClick={() => setIsOpen(false)}
        />
      )}
    </>
  )
}
