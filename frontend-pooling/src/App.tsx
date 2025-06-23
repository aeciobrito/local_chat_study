import React, { useState } from 'react';
import Login from './components/Login';
import Chat from './components/Chat';
import './App.css';

function App() {
  const [token, setToken] = useState<string | null>(null);
  const [username, setUsername] = useState<string>('');

  const handleLoginSuccess = (newToken: string, loggedInUsername: string) => {
    setToken(newToken);
    setUsername(loggedInUsername);
  };

  return (
    <div className="App">
      <header className="App-header">
        <h1>Chat App MVP (Polling)</h1>
        {!token ? (
          <Login onLoginSuccess={handleLoginSuccess} />
        ) : (
          <Chat token={token} currentUser={username} />
        )}
      </header>
    </div>
  );
}

export default App;
