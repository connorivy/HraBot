import { type FormEvent, useEffect, useRef, useState } from 'react'
import {
  Avatar,
  Box,
  Container,
  CssBaseline,
  Divider,
  IconButton,
  Paper,
  Stack,
  TextField,
  ThemeProvider,
  Typography,
  createTheme,
} from '@mui/material'
import SendRoundedIcon from '@mui/icons-material/SendRounded'
type ChatRole = 'user' | 'ai'

type ChatMessage = {
  id: number
  role: ChatRole
  text: string
  timestamp: number
}

const dummyReplies = [
  'Thanks for the note! I can draft a quick response for your manager.',
  'Got it. I will summarize the key policy points and next steps.',
  'I can help you with that. Want a short answer or a detailed breakdown?',
  'Understood. I will format this as a checklist you can forward.',
]

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#f28c28' },
    secondary: { main: '#1d7a6b' },
    background: { default: '#f6f0e9' },
    text: { primary: '#1f1d1a', secondary: '#4b4844' },
  },
  typography: {
    fontFamily: '"Space Grotesk", "Segoe UI", "Helvetica Neue", Arial, sans-serif',
    h4: {
      fontFamily: '"Fraunces", "Times New Roman", serif',
      fontWeight: 600,
      letterSpacing: '-0.5px',
    },
    overline: {
      fontWeight: 600,
      letterSpacing: '0.3em',
    },
  },
})

function App() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [inputValue, setInputValue] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const replyIndex = useRef(0)
  const streamRef = useRef<HTMLDivElement | null>(null)

  useEffect(() => {
    if (!streamRef.current) return
    streamRef.current.scrollTop = streamRef.current.scrollHeight
  }, [messages, isTyping])

  const buildDummyReply = (prompt: string) => {
    const reply = dummyReplies[replyIndex.current % dummyReplies.length]
    replyIndex.current += 1
    if (prompt.length > 80) {
      return `${reply} I can also trim this into a short summary.`
    }
    return reply
  }

  const handleSend = () => {
    const trimmed = inputValue.trim()
    if (!trimmed) return

    const timestamp = Date.now()
    const userMessage: ChatMessage = {
      id: timestamp,
      role: 'user',
      text: trimmed,
      timestamp,
    }

    setMessages((prev) => [...prev, userMessage])
    setInputValue('')
    setIsTyping(true)

    window.setTimeout(() => {
      const aiMessage: ChatMessage = {
        id: Date.now(),
        role: 'ai',
        text: buildDummyReply(trimmed),
        timestamp: Date.now(),
      }
      setMessages((prev) => [...prev, aiMessage])
      setIsTyping(false)
    }, 550)
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    handleSend()
  }

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <Box className="relative flex min-h-screen items-center justify-center overflow-hidden bg-[#f6f0e9] px-4 py-14">
        <Box
          aria-hidden="true"
          className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_10%_20%,rgba(242,140,40,0.28),transparent_55%),radial-gradient(circle_at_90%_10%,rgba(29,122,107,0.2),transparent_45%),linear-gradient(140deg,#fef7ec_0%,#f3e7d8_45%,#efe0d1_100%)]"
        />
        <Container maxWidth="sm" className="relative z-10">
          <Paper className="flex flex-col overflow-hidden rounded-[28px] bg-[#fff9f2] shadow-[0_20px_60px_rgba(31,29,26,0.18)]">
            <Box className="px-8 pb-5 pt-7 sm:px-9">
              <Typography variant="overline" className="text-[#1d7a6b]">
                HraBot
              </Typography>
              <Typography variant="h4" className="mt-1">
                HR answers, instantly.
              </Typography>
              <Typography
                variant="body2"
                className="mt-1.5 text-[rgba(31,29,26,0.7)]"
              >
                Ask a question and get a fast, friendly response.
              </Typography>
            </Box>
            <Divider />
            <Box
              ref={streamRef}
              className="flex max-h-[420px] min-h-[320px] flex-col gap-4 overflow-y-auto px-7 py-6 sm:px-8"
            >
              {messages.length === 0 ? (
                <Box className="rounded-[20px] border border-dashed border-[rgba(31,29,26,0.2)] bg-white/60 px-4 py-12 text-center">
                  <Typography variant="subtitle1">No messages yet</Typography>
                  <Typography variant="body2">
                    Start a chat to see the conversation flow.
                  </Typography>
                </Box>
              ) : (
                messages.map((message) => (
                  <Stack
                    key={message.id}
                    direction="row"
                    spacing={1.5}
                    className={`items-start ${
                      message.role === 'user'
                        ? 'flex-row-reverse self-end text-right'
                        : ''
                    }`}
                  >
                    <Avatar
                      className={`h-9 w-9 text-[0.65rem] font-semibold ${
                        message.role === 'user'
                          ? 'bg-[rgba(242,140,40,0.18)] text-[#c45b16]'
                          : 'bg-[rgba(29,122,107,0.15)] text-[#1d7a6b]'
                      }`}
                    >
                      {message.role === 'user' ? 'You' : 'HR'}
                    </Avatar>
                    <Box
                      className={`flex max-w-[70%] flex-col gap-1.5 rounded-[18px] border border-[rgba(31,29,26,0.08)] px-4 py-3 ${
                        message.role === 'user'
                          ? 'border-transparent bg-[#f28c28] text-white'
                          : 'bg-white'
                      }`}
                    >
                      <Typography variant="body1">{message.text}</Typography>
                      <Typography variant="caption" className="opacity-60">
                        {new Date(message.timestamp).toLocaleTimeString([], {
                          hour: '2-digit',
                          minute: '2-digit',
                        })}
                      </Typography>
                    </Box>
                  </Stack>
                ))
              )}
              {isTyping && (
                <Stack direction="row" spacing={1.5} className="items-start">
                  <Avatar className="h-9 w-9 bg-[rgba(29,122,107,0.15)] text-[0.65rem] font-semibold text-[#1d7a6b]">
                    HR
                  </Avatar>
                  <Box className="flex max-w-[70%] flex-col gap-1.5 rounded-[18px] border border-[rgba(31,29,26,0.08)] bg-white px-4 py-3">
                    <Box className="flex h-5 items-center gap-1.5">
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-[rgba(31,29,26,0.5)]" />
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-[rgba(31,29,26,0.5)] [animation-delay:0.15s]" />
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-[rgba(31,29,26,0.5)] [animation-delay:0.3s]" />
                    </Box>
                  </Box>
                </Stack>
              )}
            </Box>
            <Divider />
            <Box
              component="form"
              onSubmit={handleSubmit}
              className="flex items-center gap-2.5 px-5 pb-5 pt-4"
            >
              <TextField
                value={inputValue}
                onChange={(event) => setInputValue(event.target.value)}
                placeholder="Ask about time off, benefits, or policies..."
                fullWidth
                size="small"
                variant="outlined"
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    handleSend()
                  }
                }}
                inputProps={{ 'aria-label': 'Chat message' }}
              />
              <IconButton
                type="submit"
                color="primary"
                className="rounded-[14px] bg-[#f28c28] text-white shadow-[0_10px_24px_rgba(242,140,40,0.35)] transition hover:bg-[#c45b16] disabled:bg-[rgba(31,29,26,0.2)] disabled:text-[rgba(31,29,26,0.5)] disabled:shadow-none"
                disabled={!inputValue.trim()}
              >
                <SendRoundedIcon />
              </IconButton>
            </Box>
          </Paper>
        </Container>
      </Box>
    </ThemeProvider>
  )
}

export default App
