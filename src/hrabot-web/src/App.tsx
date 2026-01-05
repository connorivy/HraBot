import { type FormEvent, useEffect, useRef, useState } from 'react'
import {
  Avatar,
  Box,
  Button,
  Container,
  CssBaseline,
  Divider,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
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
import ThumbDownAltOutlinedIcon from '@mui/icons-material/ThumbDownAltOutlined'
import ThumbUpAltOutlinedIcon from '@mui/icons-material/ThumbUpAltOutlined'
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

const defaultFeedbackItems = [
  { id: 1, shortDescription: 'no issues', feedbackItem: 'MessageContent', feedbackType: 'Positive' },
  { id: 4, shortDescription: 'incorrect', feedbackItem: 'MessageContent', feedbackType: 'Negative' },
  { id: 5, shortDescription: 'missing information', feedbackItem: 'MessageContent', feedbackType: 'Negative' },
  { id: 6, shortDescription: 'not applicable to question', feedbackItem: 'MessageContent', feedbackType: 'Negative' },
  { id: 7, shortDescription: 'not informed by citations', feedbackItem: 'MessageContent', feedbackType: 'Negative' },
  { id: 8, shortDescription: 'other', feedbackItem: 'MessageContent', feedbackType: 'Negative' },
  { id: 2, shortDescription: 'no issues', feedbackItem: 'Citation', feedbackType: 'Positive' },
  { id: 9, shortDescription: 'missing', feedbackItem: 'Citation', feedbackType: 'Negative' },
  { id: 10, shortDescription: 'incorrect', feedbackItem: 'Citation', feedbackType: 'Negative' },
  { id: 11, shortDescription: 'not applicable to question', feedbackItem: 'Citation', feedbackType: 'Negative' },
  { id: 12, shortDescription: 'other', feedbackItem: 'Citation', feedbackType: 'Negative' },
  { id: 3, shortDescription: 'no issues', feedbackItem: 'Citation', feedbackType: 'Positive' },
  { id: 15, shortDescription: 'other', feedbackItem: 'Other', feedbackType: 'Negative' },
]

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
  const [conversationId, setConversationId] = useState<number | null>(null)
  const [pendingFeedback, setPendingFeedback] = useState(false)
  const [feedbackMessageId, setFeedbackMessageId] = useState<number | null>(null)
  const [feedbackUiMessageId, setFeedbackUiMessageId] = useState<number | null>(null)
  const [feedbackSubmitting, setFeedbackSubmitting] = useState(false)
  const [feedbackDialogOpen, setFeedbackDialogOpen] = useState(false)
  const [feedbackItems, setFeedbackItems] = useState<
    { id?: number | null; shortDescription?: string | null; feedbackItem?: string | null; feedbackType?: string | null }[]
  >(defaultFeedbackItems)
  const [feedbackItemsLoading, setFeedbackItemsLoading] = useState(false)
  const [feedbackItemsFetched, setFeedbackItemsFetched] = useState(false)
  const [contentSelection, setContentSelection] = useState('no issues')
  const [citationsSelection, setCitationsSelection] = useState('no issues')
  const [importanceSelection, setImportanceSelection] = useState('')
  const [additionalComments, setAdditionalComments] = useState('')
  const streamRef = useRef<HTMLDivElement | null>(null)
  const apiClient = useApiClient()

  useEffect(() => {
    if (!streamRef.current) return
    streamRef.current.scrollTop = streamRef.current.scrollHeight
  }, [messages, isTyping])

  const handleSend = async () => {
    const trimmed = inputValue.trim()
    if (!trimmed || pendingFeedback) return

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
      setMessages((prev) => [...prev, aiMessage])
      if (response?.messageId) {
        setFeedbackMessageId(response.messageId)
        setFeedbackUiMessageId(aiMessage.id)
        setPendingFeedback(true)
      }
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

  const handlePositiveFeedback = async () => {
    if (feedbackSubmitting || feedbackMessageId == null) return
    setFeedbackSubmitting(true)
    try {
      await apiClient.feedback.post({
        messageId: feedbackMessageId,
        messageFeedbackItemIds: [1, 2, 3],
        additionalComments: null,
        importanceToTakeCommand: null,
      })
      setPendingFeedback(false)
      setFeedbackUiMessageId(null)
    } catch (error) {
      console.error('Failed to submit feedback.', error)
    } finally {
      setFeedbackSubmitting(false)
    }
  }

  const handleOpenNegativeFeedback = async () => {
    if (feedbackSubmitting || feedbackMessageId == null) return
    setFeedbackDialogOpen(true)
    setContentSelection('no issues')
    setCitationsSelection('no issues')
    setImportanceSelection('')
    setAdditionalComments('')
    if (feedbackItemsLoading || feedbackItemsFetched) return
    setFeedbackItemsLoading(true)
    try {
      const response = await apiClient.feedback.items.get()
      if (response?.length) {
        setFeedbackItems(response)
        setFeedbackItemsFetched(true)
      }
    } catch (error) {
      console.error('Failed to load feedback options.', error)
    } finally {
      setFeedbackItemsLoading(false)
    }
  }

  const handleSubmitNegativeFeedback = async () => {
    if (feedbackSubmitting || feedbackMessageId == null) return
    const selectedIds = [contentSelection, citationsSelection]
      .filter((value) => value !== 'no issues')
      .map((value) => Number.parseInt(value, 10))
      .filter((value) => Number.isFinite(value))

    const importanceValue = Number.parseInt(importanceSelection, 10)
    if (selectedIds.length === 0 || !Number.isFinite(importanceValue)) return

    setFeedbackSubmitting(true)
    try {
      await apiClient.feedback.post({
        messageId: feedbackMessageId,
        messageFeedbackItemIds: selectedIds,
        additionalComments: additionalComments.trim() || null,
        importanceToTakeCommand: importanceValue,
      })
      setPendingFeedback(false)
      setFeedbackUiMessageId(null)
      setFeedbackDialogOpen(false)
    } catch (error) {
      console.error('Failed to submit feedback.', error)
    } finally {
      setFeedbackSubmitting(false)
    }
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    void handleSend()
  }

  const feedbackMessage = messages.find((message) => message.id === feedbackUiMessageId)
  const negativeFeedbackOptions = feedbackItems.filter(
    (item) => item.feedbackType?.toLowerCase() === 'negative',
  )
  const contentOptions = negativeFeedbackOptions.filter(
    (item) => item.feedbackItem?.toLowerCase() === 'messagecontent',
  )
  const citationOptions = negativeFeedbackOptions.filter(
    (item) => item.feedbackItem?.toLowerCase() === 'citation',
  )
  const canSubmitNegativeFeedback =
    !feedbackSubmitting &&
    [contentSelection, citationsSelection].some((value) => value !== 'no issues') &&
    importanceSelection !== ''

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
                      {pendingFeedback &&
                        message.role === 'ai' &&
                        feedbackUiMessageId === message.id ? (
                        <Box className="mt-2 flex gap-1.5">
                          <IconButton
                            color="primary"
                            aria-label="Thumbs up"
                            disabled={feedbackSubmitting}
                            onClick={() => void handlePositiveFeedback()}
                          >
                            <ThumbUpAltOutlinedIcon />
                          </IconButton>
                          <IconButton
                            color="secondary"
                            aria-label="Thumbs down"
                            disabled={feedbackSubmitting}
                            onClick={() => void handleOpenNegativeFeedback()}
                          >
                            <ThumbDownAltOutlinedIcon />
                          </IconButton>
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
            <Divider />
            <Dialog
              open={feedbackDialogOpen}
              onClose={() => {
                if (feedbackSubmitting) return
                setFeedbackDialogOpen(false)
              }}
              fullWidth
              maxWidth="sm"
            >
              <DialogTitle>Tell us what went wrong</DialogTitle>
              <DialogContent className="flex flex-col gap-3">
                <Box className="rounded-xl border px-3 py-2" sx={{ borderColor: 'divider' }}>
                  <Typography variant="subtitle2" color="text.secondary">
                    AI response
                  </Typography>
                  <Typography variant="body2" className="mt-1">
                    {feedbackMessage?.text ?? 'Response unavailable.'}
                  </Typography>
                  {feedbackMessage?.citations?.length ? (
                    <CitationList citations={feedbackMessage.citations} />
                  ) : null}
                </Box>
                <p></p>
                <p></p>
                <p></p>
                <Box className="grid gap-3 sm:grid-cols-2">
                  <TextField
                    select
                    label="Content"
                    value={contentSelection}
                    onChange={(event) => setContentSelection(event.target.value)}
                    SelectProps={{ native: true }}
                    fullWidth
                  >
                    <option value="no issues">no issues</option>
                    {contentOptions.map((item) => (
                      <option key={item.id} value={item.id ?? ''}>
                        {item.shortDescription}
                      </option>
                    ))}
                  </TextField>
                  <TextField
                    select
                    label="Citations"
                    value={citationsSelection}
                    onChange={(event) => setCitationsSelection(event.target.value)}
                    SelectProps={{ native: true }}
                    fullWidth
                  >
                    <option value="no issues">no issues</option>
                    {citationOptions.map((item) => (
                      <option key={item.id} value={item.id ?? ''}>
                        {item.shortDescription}
                      </option>
                    ))}
                  </TextField>
                  <TextField
                    select
                    label="Importance"
                    value={importanceSelection}
                    onChange={(event) => setImportanceSelection(event.target.value)}
                    SelectProps={{ native: true, displayEmpty: true }}
                    InputLabelProps={{ shrink: true }}
                    fullWidth
                    helperText="How important is providing a correct answer to this question"
                  >
                    <option value="" disabled>
                      Select importance
                    </option>
                    {[1, 2, 3, 4, 5].map((value) => (
                      <option key={value} value={value}>
                        {value} - {value === 1 ? 'Least' : value === 5 ? 'Most' : 'Moderate'} importance
                      </option>
                    ))}
                  </TextField>
                </Box>
                <TextField
                  label="Other comments (optional)"
                  value={additionalComments}
                  onChange={(event) => setAdditionalComments(event.target.value)}
                  placeholder="Share anything else about the response"
                  fullWidth
                  multiline
                  minRows={3}
                />
              </DialogContent>
              <DialogActions className="px-6 pb-4">
                <Button
                  variant="contained"
                  color="primary"
                  onClick={() => void handleSubmitNegativeFeedback()}
                  disabled={!canSubmitNegativeFeedback}
                >
                  Submit
                </Button>
              </DialogActions>
            </Dialog>
            <Box
              component="form"
              onSubmit={handleSubmit}
              className="flex items-center gap-2.5"
              sx={{ px: { xs: 2, sm: 3 }, pb: 2.5, pt: 2 }}
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
                    disabled={pendingFeedback}
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
                disabled={pendingFeedback || !inputValue.trim()}
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
