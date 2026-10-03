import { useCallback, useEffect, useState } from 'react'
import { getComments } from './api/client'
import type { CommentDto, PagedResult, SortDir, SortField } from './api/types'
import { CommentForm } from './components/CommentForm'
import { CommentsTable } from './components/CommentsTable'
import { Pagination } from './components/Pagination'
import { useCommentsHub } from './hooks/useCommentsHub'
import type { HubEvent } from './hooks/useCommentsHub'

export default function App() {
    const [page, setPage] = useState(1)
    const [sortBy, setSortBy] = useState<SortField>('createdAt')
    const [sortDir, setSortDir] = useState<SortDir>('desc')
    const [data, setData] = useState<PagedResult<CommentDto> | null>(null)
    const [error, setError] = useState<string | null>(null)
    const [loading, setLoading] = useState(false)
    const [showForm, setShowForm] = useState(false)
    const [replyingTo, setReplyingTo] = useState<number | null>(null)
    const [highlightId, setHighlightId] = useState<number | null>(null)

    const load = useCallback(async () => {
        setLoading(true)
        try {
            setData(await getComments(page, sortBy, sortDir))
            setError(null)
        } catch {
            setError('Could not load comments. Is the API running?')
        } finally {
            setLoading(false)
        }
    }, [page, sortBy, sortDir])

    useEffect(() => {
        void load()
    }, [load])

    // live updates over WebSocket
    const onHubEvent = useCallback(
        (event: HubEvent) => {
            if (event.type === 'commentCreated') setHighlightId(event.commentId)
            void load()
        },
        [load],
    )
    const connected = useCommentsHub(onHubEvent)

    useEffect(() => {
        if (highlightId === null) return
        const timer = setTimeout(() => setHighlightId(null), 3000)
        return () => clearTimeout(timer)
    }, [highlightId])

    const handleSort = (field: SortField) => {
        if (field === sortBy) {
            setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
        } else {
            setSortBy(field)
            setSortDir(field === 'createdAt' ? 'desc' : 'asc')
        }
        setPage(1)
    }

    const handleCreated = (comment: CommentDto) => {
        setShowForm(false)
        setReplyingTo(null)
        setHighlightId(comment.id)
        if (comment.parentId === null) {
            // new top-level comment is newest, so show the first page in default (LIFO) order
            setSortBy('createdAt')
            setSortDir('desc')
            setPage(1)
        }
        void load()
    }

    return (
        <div className="container">
            <header className="page-header">
                <h1>Comments</h1>
                <span className={`live ${connected ? 'live--on' : ''}`}>{connected ? '● Live' : '○ Offline'}</span>
                <span className="spacer" />
                <button type="button" className="btn" onClick={() => setShowForm((s) => !s)}>
                    {showForm ? 'Close' : '+ Add comment'}
                </button>
            </header>

            {showForm && <CommentForm onSuccess={handleCreated} onCancel={() => setShowForm(false)} />}

            {error && <div className="alert">{error}</div>}

            {data && data.items.length === 0 && !loading && <p className="muted">No comments yet. Be the first!</p>}

            {data && data.items.length > 0 && (
                <CommentsTable
                    items={data.items}
                    sortBy={sortBy}
                    sortDir={sortDir}
                    onSort={handleSort}
                    highlightId={highlightId}
                    replyingTo={replyingTo}
                    onReplyToggle={(id) => setReplyingTo((current) => (current === id ? null : id))}
                    renderReplyForm={(parentId) => (
                        <CommentForm parentId={parentId} onSuccess={handleCreated} onCancel={() => setReplyingTo(null)} />
                    )}
                />
            )}

            {data && (
                <>
                    <Pagination page={data.page} totalPages={data.totalPages} onChange={setPage} />
                    <p className="muted small center">
                        {data.totalCount} top-level comments · page {data.page} of {Math.max(1, data.totalPages)}
                    </p>
                </>
            )}
        </div>
    )
}