import { type FormEvent, useEffect, useRef, useState } from 'react'
import {
  Avatar,
  Box,
  Container,
  CssBaseline,
  Divider,
  Button,
  IconButton,
  Paper,
  Stack,
  TextField,
  ThemeProvider,
  Tooltip,
  Typography,
  createTheme,
} from '@mui/material'
import SendRoundedIcon from '@mui/icons-material/SendRounded'
import StarBorderRoundedIcon from '@mui/icons-material/StarBorderRounded'
import StarRoundedIcon from '@mui/icons-material/StarRounded'
import LaunchRoundedIcon from '@mui/icons-material/LaunchRounded'
import EditRoundedIcon from '@mui/icons-material/EditRounded'
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

const ratingOptions = [
  { value: 1, label: 'Very poor' },
  { value: 2, label: 'Poor' },
  { value: 3, label: 'Okay' },
  { value: 4, label: 'Good' },
  { value: 5, label: 'Excellent' },
]

const importanceOptions = [
  { value: 1, label: '1' },
  { value: 2, label: '2' },
  { value: 3, label: '3' },
  { value: 4, label: '4' },
  { value: 5, label: '5' },
]

const FEEDBACK_BUTTON_SIZE = 42
const USER_MESSAGE_LIMIT = 5

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
              className="group flex items-center justify-between gap-3 rounded-lg border border-dashed px-3 py-2 text-left text-xs font-medium hover:border-solid"
              sx={{
                color: 'primary.main',
                textDecoration: 'none',
                borderLeft: '3px solid',
                borderColor: 'primary.main',
                bgcolor: 'rgba(24, 138, 104, 0.05)',
              }}
            >
              <Box className="flex flex-1 flex-col gap-0.5">
                <Box
                  component="span"
                  className="text-[0.65rem] uppercase tracking-[0.24em]"
                  sx={{ color: 'primary.main' }}
                >
                  Source
                </Box>
                <Typography variant="body2" className="text-sm text-current">
                  {quote}
                </Typography>
              </Box>
              <LaunchRoundedIcon fontSize="small" className="opacity-70 transition group-hover:opacity-100" />
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
  const [conversationId, setConversationId] = useState<number | null>(null)
  const [pendingFeedback, setPendingFeedback] = useState(false)
  const [feedbackMessageId, setFeedbackMessageId] = useState<number | null>(null)
  const [feedbackUiMessageId, setFeedbackUiMessageId] = useState<number | null>(null)
  const [feedbackSubmitting, setFeedbackSubmitting] = useState(false)
  const [ratingSelection, setRatingSelection] = useState<number | null>(null)
  const [importanceSelection, setImportanceSelection] = useState('')
  const [userMessageCount, setUserMessageCount] = useState(0)
  const [limitReached, setLimitReached] = useState(false)
  const streamRef = useRef<HTMLDivElement | null>(null)
  const feedbackQueueRef = useRef<{ rating: number; importance: number } | null>(null)
  const apiClient = useApiClient()

  useEffect(() => {
    const warmUpBackend = async () => {
      try {
        await apiClient.pingme.get()
      } catch (error) {
        console.warn('Failed to warm up backend.', error)
      }
    }
    void warmUpBackend()
  }, [apiClient])

  useEffect(() => {
    if (!streamRef.current) return
    streamRef.current.scrollTop = streamRef.current.scrollHeight
  }, [messages, isTyping])

  const handleSend = async () => {
    const trimmed = inputValue.trim()
    if (!trimmed || pendingFeedback || limitReached) return

    const nextUserMessageCount = userMessageCount + 1
    setUserMessageCount(nextUserMessageCount)

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
      const response = await apiClient.chat.post({
        content: trimmed,
        conversationId: conversationId ?? undefined,
      })
      if (response?.conversationId != null) {
        setConversationId(response.conversationId)
      }
      const responseText =
        response?.response?.trim() ??
        'Sorry, I could not find a response right now.'
      const aiMessage: ChatMessage = {
        id: response?.messageId ?? Date.now(),
        role: 'ai',
        text: responseText,
        timestamp: Date.now(),
        citations: response?.citations ?? undefined,
      }
      setMessages((prev) => {
        const updated = [...prev, aiMessage]
        if (nextUserMessageCount >= USER_MESSAGE_LIMIT) {
          updated.push({
            id: Date.now() + 1,
            role: 'ai',
            text: 'You have reached the message limit for this chat. Please start a new conversation.',
            timestamp: Date.now(),
          })
        }
        return updated
      })
      if (nextUserMessageCount >= USER_MESSAGE_LIMIT) {
        setLimitReached(true)
      }
      if (response?.messageId) {
        setFeedbackMessageId(response.messageId)
        setFeedbackUiMessageId(aiMessage.id)
        setPendingFeedback(true)
        feedbackQueueRef.current = null
        setRatingSelection(null)
        setImportanceSelection('')
      }
    } catch (error) {
      console.error('Failed to fetch response from HraBot.', error)
      const aiMessage: ChatMessage = {
        id: Date.now(),
        role: 'ai',
        text: 'Sorry, I ran into an issue contacting the HraBot service.',
        timestamp: Date.now(),
      }
      setMessages((prev) => {
        const updated = [...prev, aiMessage]
        if (nextUserMessageCount >= USER_MESSAGE_LIMIT) {
          updated.push({
            id: Date.now() + 1,
            role: 'ai',
            text: 'You have reached the message limit for this chat. Please start a new conversation.',
            timestamp: Date.now(),
          })
        }
        return updated
      })
      if (nextUserMessageCount >= USER_MESSAGE_LIMIT) {
        setLimitReached(true)
      }
    } finally {
      setIsTyping(false)
    }
  }

  const processQueuedFeedback = async () => {
    if (feedbackMessageId == null) return
    const payload = feedbackQueueRef.current
    if (!payload) return
    feedbackQueueRef.current = null
    setFeedbackSubmitting(true)
    try {
      await apiClient.feedback.put({
        messageId: feedbackMessageId,
        rating: payload.rating,
        importanceToTakeCommand: payload.importance,
      })
      const hasQueuedUpdate = feedbackQueueRef.current !== null
      if (!hasQueuedUpdate) {
        setPendingFeedback(false)
      }
    } catch (error) {
      console.error('Failed to submit feedback.', error)
    } finally {
      setFeedbackSubmitting(false)
      if (feedbackQueueRef.current) {
        void processQueuedFeedback()
      }
    }
  }

  const queueFeedbackSubmission = (ratingValue: number | null, importanceValue: string) => {
    if (feedbackMessageId == null) return
    const parsedImportance = Number.parseInt(importanceValue, 10)
    const isValidRating = ratingValue != null && ratingValue >= 1 && ratingValue <= 5
    const isValidImportance =
      Number.isFinite(parsedImportance) && parsedImportance >= 1 && parsedImportance <= 5
    if (!isValidRating || !isValidImportance || ratingValue == null) return
    feedbackQueueRef.current = { rating: ratingValue, importance: parsedImportance }
    if (!feedbackSubmitting) {
      void processQueuedFeedback()
    }
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void handleSend()
  }

  const handleNewChat = () => {
    setMessages([])
    setInputValue('')
    setConversationId(null)
    setPendingFeedback(false)
    setFeedbackMessageId(null)
    setFeedbackUiMessageId(null)
    setFeedbackSubmitting(false)
    setRatingSelection(null)
    setImportanceSelection('')
    setIsTyping(false)
    setUserMessageCount(0)
    setLimitReached(false)
    feedbackQueueRef.current = null
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
                HRA answers, instantly.
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
                      {message.role === 'user' ? 'You' : 'HB'}
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
                      {message.role === 'ai' && feedbackUiMessageId === message.id ? (
                        <Box
                          className="mt-3 flex flex-col gap-2.5"
                          sx={{
                            borderRadius: 3,
                            border: 1,
                            borderColor: 'primary.main',
                            px: { xs: 2, sm: 2.5 },
                            py: { xs: 1.75, sm: 2.25 },
                            boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.4)',
                          }}
                        >
                          <Box className="flex items-center gap-1.5">
                            <Typography variant="subtitle2" color="primary">
                              Rate this response
                            </Typography>
                          </Box>
                          <Stack spacing={2.25}>
                            <Box className="flex flex-col gap-1.5">
                              <Typography variant="body2" color="text.secondary">
                                Give an overall rating based on information accuracy and citation quality.
                              </Typography>
                              <Box className="flex items-center gap-2">
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                  sx={{ minWidth: 96, textAlign: 'right' }}
                                >
                                </Typography>
                                <Box
                                  sx={{
                                    display: 'grid',
                                    gridTemplateColumns: `repeat(5, ${FEEDBACK_BUTTON_SIZE}px)`,
                                    gap: 1,
                                    justifyItems: 'center',
                                  }}
                                  role="group"
                                  aria-label="Response rating"
                                >
                                  {ratingOptions.map(({ value, label }) => {
                                    const isFilled = ratingSelection != null && value <= ratingSelection
                                    const isSelected = ratingSelection === value
                                    return (
                                      <IconButton
                                        key={value}
                                        color={isFilled ? 'warning' : 'default'}
                                        aria-label={label}
                                        aria-pressed={isSelected}
                                        onClick={() => {
                                          setRatingSelection(value)
                                          queueFeedbackSubmission(value, importanceSelection)
                                        }}
                                        size="small"
                                        sx={{
                                          bgcolor: isSelected ? 'rgba(255, 213, 79, 0.2)' : undefined,
                                          borderRadius: 2,
                                          width: FEEDBACK_BUTTON_SIZE,
                                          height: FEEDBACK_BUTTON_SIZE,
                                        }}
                                      >
                                        {isFilled ? <StarRoundedIcon /> : <StarBorderRoundedIcon />}
                                      </IconButton>
                                    )
                                  })}
                                </Box>
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                  sx={{ minWidth: 96 }}
                                >
                                  {/* Excellent */}
                                </Typography>
                              </Box>
                            </Box>
                            <Box className="flex flex-col gap-1.5">
                              <Typography variant="body2" color="text.secondary">
                                How important is providing an accurate answer to this question?
                              </Typography>
                              <Box className="flex flex-wrap items-center gap-2">
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                  sx={{ minWidth: 96, textAlign: 'right' }}
                                >
                                  Not important
                                </Typography>
                                <Box
                                  className="flex items-center"
                                  role="group"
                                  aria-label="Response importance"
                                  sx={{
                                    display: 'grid',
                                    gridTemplateColumns: `repeat(5, ${FEEDBACK_BUTTON_SIZE}px)`,
                                    gap: 1,
                                    justifyItems: 'center',
                                  }}
                                >
                                  {importanceOptions.map(({ value, label }) => {
                                    const isSelected = Number(importanceSelection) === value
                                    return (
                                      <IconButton
                                        key={value}
                                        aria-label={`Importance ${value}`}
                                        aria-pressed={isSelected}
                                        color={isSelected ? 'primary' : 'default'}
                                        onClick={() => {
                                          setImportanceSelection(String(value))
                                          queueFeedbackSubmission(ratingSelection, String(value))
                                        }}
                                        size="small"
                                        sx={{
                                          borderRadius: 2,
                                          border: 1,
                                          borderColor: isSelected ? 'primary.main' : 'divider',
                                          bgcolor: isSelected ? 'rgba(24,138,104,0.12)' : undefined,
                                          width: FEEDBACK_BUTTON_SIZE,
                                          height: FEEDBACK_BUTTON_SIZE,
                                        }}
                                      >
                                        {label}
                                      </IconButton>
                                    )
                                  })}
                                </Box>
                                <Typography
                                  variant="caption"
                                  color="text.secondary"
                                  sx={{ minWidth: 96 }}
                                >
                                  Extremely important
                                </Typography>
                              </Box>
                            </Box>
                          </Stack>
                        </Box>
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
                    data-testid="assistant-typing"
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
            <Box
              className="flex items-center justify-center"
              sx={{ px: { xs: 2, sm: 3 }, pb: 1.5 }}
            >
              <Tooltip
                title="Rate the last response to continue."
                disableHoverListener={!pendingFeedback}
              >
                <span>
                  <Button
                    variant="outlined"
                    color="primary"
                    startIcon={<EditRoundedIcon />}
                    onClick={handleNewChat}
                    disabled={pendingFeedback && !limitReached}
                  >
                    New chat
                  </Button>
                </span>
              </Tooltip>
            </Box>
            <Divider />
            <Box
              component="form"
              onSubmit={handleSubmit}
              className="flex items-center gap-2.5"
              sx={{ px: { xs: 2, sm: 3 }, pb: 2.5, pt: 1 }}
            >
              <Tooltip
                title="Rate the last response to continue."
                disableHoverListener={!pendingFeedback}
              >
                <span className="flex-1">
                  <TextField
                    value={inputValue}
                    onChange={(event) => setInputValue(event.target.value)}
                    placeholder="Ask about time off, benefits, or policies..."
                    fullWidth
                    size="small"
                    variant="outlined"
                    color="primary"
                    disabled={pendingFeedback || limitReached}
                    onKeyDown={(event) => {
                      if (event.key === 'Enter' && !event.shiftKey) {
                        event.preventDefault()
                        void handleSend()
                      }
                    }}
                    inputProps={{ 'aria-label': 'Chat message' }}
                  />
                </span>
              </Tooltip>
              <IconButton
                type="submit"
                aria-label="Send message"
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
                disabled={pendingFeedback || limitReached || !inputValue.trim()}
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
