import React, { useState, useEffect, useRef } from 'react';
import axios from 'axios';

interface Message {
  id: string;
  sender: string;
  receiver: string;
  content: string;
  timestamp: string;
}

interface ChatProps {
  token: string;
  currentUser: string;
}

const Chat: React.FC<ChatProps> = ({ token, currentUser }) => {
  const [messages, setMessages] = useState<Message[]>([]);
  const [newMessage, setNewMessage] = useState('');

  // O outro usuário da conversa. Para este MVP, ele é fixo.
  const otherUser = currentUser === 'aecio' ? 'roberta' : 'aecio';

  // --- AQUI ESTÁ O POLLING MECHANISM ---
  useEffect(() => {
    const fetchMessages = async () => {
      try {
        const response = await axios.get(`http://localhost:5239/api/messages/${otherUser}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        setMessages(response.data);
      } catch (error) {
        console.error('Falha ao buscar mensagens:', error);
      }
    };

    fetchMessages(); // Busca inicial
    const intervalId = setInterval(fetchMessages, 3000); // Polling a cada 3 segundos

    // Função de limpeza para parar o polling quando o componente for desmontado
    return () => clearInterval(intervalId);
  }, [token, otherUser]); // Dependências do useEffect

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newMessage.trim()) return;

    try {
      await axios.post('http://localhost:5239/api/messages', 
        { receiver: otherUser, content: newMessage },
        { headers: { Authorization: `Bearer ${token}` } }
      );
      setNewMessage('');
      // A mensagem aparecerá na próxima busca do polling, não precisa adicionar manualmente ao estado.
    } catch (error) {
      console.error('Falha ao enviar mensagem:', error);
    }
  };

  return (
    <div>
      <h2>Chat com {otherUser}</h2>
      <div className="message-list" style={{ height: '300px', border: '1px solid #ccc', overflowY: 'scroll', padding: '10px' }}>
        {messages.map((msg) => (
          <div key={msg.id} style={{ textAlign: msg.sender === currentUser ? 'right' : 'left' }}>
            <p>
              <strong>{msg.sender}:</strong> {msg.content}
            </p>
          </div>
        ))}
      </div>
      <form onSubmit={handleSendMessage}>
        <input
          type="text"
          value={newMessage}
          onChange={(e) => setNewMessage(e.target.value)}
          placeholder="Digite sua mensagem"
          style={{ width: '80%' }}
        />
        <button type="submit">Enviar</button>
      </form>
    </div>
  );
};

export default Chat;