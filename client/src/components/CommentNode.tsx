import type { ReactNode } from 'react'
import type { CommentDto } from '../api/types'
import { formatDate } from '../utils/format'
import { AttachmentView } from './AttachmentView'
import { AuthorName, Avatar } from './Avatar'

export type CommentNodeShared = {
    highlightId: number | null
    replyingTo: number | null
    onReplyToggle: (id: number) => void
    renderReplyForm: (parentId: number) => ReactNode
}

type Props = CommentNodeShared & { comment: CommentDto; isRoot?: boolean }

export function CommentNode({ comment, isRoot = false, ...shared }: Props) {
    const { highlightId, replyingTo, onReplyToggle, renderReplyForm } = shared
    const isReplying = replyingTo === comment.id
    const isNew = !isRoot && comment.id === highlightId

    return (
        <div className={`comment${isRoot ? ' comment--root' : ''}${isNew ? ' is-new' : ''}`}>
            {!isRoot && (
                <div className="comment-header">
                    <Avatar name={comment.userName} />
                    <AuthorName name={comment.userName} homePage={comment.homePage} />
                    <span className="comment-date">{formatDate(comment.createdAt)}</span>
                </div>
            )}

            {/* Safe: the server's whitelist sanitizer allows only <a>, <code>, <i>, <strong> and encodes everything else */}
            <div className="comment-text" dangerouslySetInnerHTML={{ __html: comment.text }} />

            {comment.attachment && <AttachmentView attachment={comment.attachment} />}

            <div className="comment-actions">
                <button type="button" className="link-btn" onClick={() => onReplyToggle(comment.id)}>
                    {isReplying ? 'Cancel' : '↩ Reply'}
                </button>
            </div>

            {isReplying && <div className="reply-form">{renderReplyForm(comment.id)}</div>}

            {comment.replies.length > 0 && (
                <div className="comment-replies">
                    {comment.replies.map((reply) => (
                        <CommentNode key={reply.id} comment={reply} {...shared} />
                    ))}
                </div>
            )}
        </div>
    )
}