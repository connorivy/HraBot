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
import { ApiClientProvider, useApiClient } from './features/ApiClientProvider'
type ChatRole = 'user' | 'ai'

type Citation = {
  filename?: string | null
  quote?: string | null
}

type ChatMessage = {
  id: number
  role: ChatRole
  text: string
  timestamp: number
  citations?: Citation[]
}

const theme = createTheme({
  palette: {
    mode: 'light',
    primary: { main: '#188A68' },
    secondary: { main: '#f28c28' },
    background: { default: '#f6f0e9', paper: '#fff9f2' },
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

function CitationList({ citations }: { citations: Citation[] }) {
  return (
    <Box className="mt-1 flex flex-col gap-1.5">
      {citations
        .filter((citation) => citation.filename && citation.quote)
        .map((citation, index) => {
          const filename = citation.filename as string
          const quote = citation.quote as string
          const href = `https://intercom.help/take-command-health/en/articles/${encodeURIComponent(filename)}`
          return (
            <Box
              key={`${filename}-${index}`}
              component="a"
              href={href}
              target="_blank"
              rel="noreferrer"
              className="rounded-md border border-dashed px-3 py-2 text-left text-xs font-medium hover:border-solid"
              sx={{ color: 'primary.main', textDecoration: 'none' }}
            >
              <Box component="span" className="block text-[0.65rem] uppercase tracking-[0.24em]">
                Source
              </Box>
              <Typography variant="body2" className="mt-1 text-sm text-current">
                {quote}
              </Typography>
            </Box>
          )
        })}
    </Box>
  )
}

function ChatPane() {
  const [messages, setMessages] = useState<ChatMessage[]>([])
  const [inputValue, setInputValue] = useState('')
  const [isTyping, setIsTyping] = useState(false)
  const streamRef = useRef<HTMLDivElement | null>(null)
  const apiClient = useApiClient()

  useEffect(() => {
    if (!streamRef.current) return
    streamRef.current.scrollTop = streamRef.current.scrollHeight
  }, [messages, isTyping])

  const handleSend = async () => {
    const trimmed = inputValue.trim()
    if (!trimmed) return

    const timestamp = Date.now()
    const userMessage: ChatMessage = {
      id: timestamp,
      role: 'user',
      text: trimmed,
      timestamp,
    }

    const nextMessages = [...messages, userMessage]
    setMessages(nextMessages)
    setInputValue('')
    setIsTyping(true)

    try {
      const response = await apiClient.api.hrabot.post({
        messages: nextMessages.map((message) => ({
          role: message.role === 'user' ? 2 : 1,
          text: message.text,
        })),
      })
      const responseText =
        response?.response?.trim() ??
        'Sorry, I could not find a response right now.'
      const aiMessage: ChatMessage = {
        id: Date.now(),
        role: 'ai',
        text: responseText,
        timestamp: Date.now(),
        citations: response?.citations ?? undefined,
      }
      setMessages((prev) => [...prev, aiMessage])
    } catch (error) {
      console.error('Failed to fetch response from HraBot.', error)
      const aiMessage: ChatMessage = {
        id: Date.now(),
        role: 'ai',
        text: 'Sorry, I ran into an issue contacting the HraBot service.',
        timestamp: Date.now(),
      }
      setMessages((prev) => [...prev, aiMessage])
    } finally {
      setIsTyping(false)
    }
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void handleSend()
  }

  return (
    <>
      <CssBaseline />
      <Box
        className="flex h-screen items-stretch justify-center px-4"
        sx={{ bgcolor: 'background.default' }}
      >
        <Container maxWidth={false} className="flex h-full w-full">
          <Paper
            elevation={10}
            className="mx-auto flex h-full w-full max-w-5xl flex-col overflow-hidden"
            sx={{ borderRadius: 4, bgcolor: 'background.paper' }}
          >
            <Box sx={{ px: { xs: 3, sm: 4 }, pt: 3, pb: 2 }}>
              <Typography variant="overline" color="primary">
                HraBot
              </Typography>
              <Typography variant="h4" className="mt-1">
                HR answers, instantly.
              </Typography>
              <Typography variant="body2" color="text.secondary" className="mt-1.5">
                Ask a question and get a fast, friendly response.
              </Typography>
            </Box>
            <Divider />
            <Box
              ref={streamRef}
              className="flex flex-1 flex-col gap-4 overflow-y-auto"
              sx={{ px: { xs: 3, sm: 4 }, py: 3 }}
            >
              {messages.length === 0 ? (
                <Paper
                  variant="outlined"
                  className="px-4 py-12 text-center"
                  sx={{
                    borderRadius: 4,
                    borderStyle: 'dashed',
                    bgcolor: 'background.paper',
                  }}
                >
                  <Typography variant="subtitle1">No messages yet</Typography>
                  <Typography variant="body2">
                    Start a chat to see the conversation flow.
                  </Typography>
                </Paper>
              ) : (
                messages.map((message) => (
                  <Stack
                    key={message.id}
                    direction="row"
                    spacing={1.5}
                    className={`items-start ${message.role === 'user'
                      ? 'flex-row-reverse self-end text-right'
                      : ''
                      }`}
                  >
                    <Avatar
                      className="h-9 w-9 text-[0.65rem] font-semibold"
                      sx={
                        message.role === 'user'
                          ? {
                            bgcolor: 'primary.main',
                            color: 'primary.contrastText',
                          }
                          : {
                            bgcolor: 'grey.800',
                            color: 'common.white',
                          }
                      }
                    >
                      {message.role === 'user' ? 'You' : 'HR'}
                    </Avatar>
                    <Box
                      className="flex max-w-[70%] flex-col gap-1.5 px-4 py-3"
                      sx={
                        message.role === 'user'
                          ? {
                            borderRadius: 3,
                            bgcolor: 'primary.main',
                            color: 'primary.contrastText',
                          }
                          : {
                            borderRadius: 3,
                            border: 1,
                            borderColor: 'divider',
                            bgcolor: 'background.paper',
                          }
                      }
                    >
                      <Typography variant="body1">{message.text}</Typography>
                      {message.role === 'ai' && message.citations?.length ? (
                        <CitationList citations={message.citations} />
                      ) : null}
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
                  <Avatar
                    className="h-9 w-9 text-[0.65rem] font-semibold"
                    sx={{ bgcolor: 'grey.800', color: 'common.white' }}
                  >
                    HR
                  </Avatar>
                  <Box
                    className="flex max-w-[70%] flex-col gap-1.5 px-4 py-3"
                    sx={{
                      borderRadius: 3,
                      border: 1,
                      borderColor: 'divider',
                      bgcolor: 'background.paper',
                    }}
                  >
                    <Box className="flex h-5 items-center gap-1.5" sx={{ color: 'text.secondary' }}>
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-current" />
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-current [animation-delay:0.15s]" />
                      <span className="h-1.5 w-1.5 animate-[typing-bounce_1s_ease-in-out_infinite] rounded-full bg-current [animation-delay:0.3s]" />
                    </Box>
                  </Box>
                </Stack>
              )}
            </Box>
            <Divider />
            <Box
              component="form"
              onSubmit={handleSubmit}
              className="flex items-center gap-2.5"
              sx={{ px: { xs: 2, sm: 3 }, pb: 2.5, pt: 2 }}
            >
              <TextField
                value={inputValue}
                onChange={(event) => setInputValue(event.target.value)}
                placeholder="Ask about time off, benefits, or policies..."
                fullWidth
                size="small"
                variant="outlined"
                color="primary"
                onKeyDown={(event) => {
                  if (event.key === 'Enter' && !event.shiftKey) {
                    event.preventDefault()
                    void handleSend()
                  }
                }}
                inputProps={{ 'aria-label': 'Chat message' }}
              />
              <IconButton
                type="submit"
                color="primary"
                sx={(muiTheme) => ({
                  bgcolor: muiTheme.palette.primary.main,
                  color: muiTheme.palette.primary.contrastText,
                  borderRadius: 3,
                  boxShadow: muiTheme.shadows[3],
                  '&:hover': { bgcolor: muiTheme.palette.primary.dark },
                  '&.Mui-disabled': {
                    bgcolor: muiTheme.palette.action.disabledBackground,
                    color: muiTheme.palette.action.disabled,
                    boxShadow: 'none',
                  },
                })}
                disabled={!inputValue.trim()}
              >
                <SendRoundedIcon />
              </IconButton>
            </Box>
          </Paper>
        </Container>
      </Box>
    </>
  )
}

function App() {
  return (
    <ThemeProvider theme={theme}>
      <ApiClientProvider>
        <ChatPane />
      </ApiClientProvider>
    </ThemeProvider>
  )
}

export default App
