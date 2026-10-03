import { Fragment } from 'react'
import type { CommentDto, SortDir, SortField } from '../api/types'
import { formatDate } from '../utils/format'
import { AuthorName, Avatar } from './Avatar'
import { CommentNode } from './CommentNode'
import type { CommentNodeShared } from './CommentNode'

type Props = CommentNodeShared & {
    items: CommentDto[]
    sortBy: SortField
    sortDir: SortDir
    onSort: (field: SortField) => void
}

const columns: { field: SortField; label: string }[] = [
    { field: 'userName', label: 'User Name' },
    { field: 'email', label: 'E-mail' },
    { field: 'createdAt', label: 'Date' },
]

export function CommentsTable({ items, sortBy, sortDir, onSort, ...shared }: Props) {
    return (
        <div className="table-wrap">
            <table className="comments-table">
                <thead>
                <tr>
                    {columns.map((col) => {
                        const active = col.field === sortBy
                        return (
                            <th key={col.field} aria-sort={active ? (sortDir === 'asc' ? 'ascending' : 'descending') : 'none'}>
                                <button
                                    type="button"
                                    className={`sort-btn${active ? ' sort-btn--active' : ''}`}
                                    onClick={() => onSort(col.field)}
                                >
                                    {col.label}
                                    <span className="sort-icon">{active ? (sortDir === 'asc' ? '▲' : '▼') : '↕'}</span>
                                </button>
                            </th>
                        )
                    })}
                </tr>
                </thead>
                <tbody>
                {items.map((c) => (
                    <Fragment key={c.id}>
                        <tr className={`row-summary${c.id === shared.highlightId ? ' is-new' : ''}`}>
                            <td>
                                <div className="author-cell">
                                    <Avatar name={c.userName} />
                                    <AuthorName name={c.userName} homePage={c.homePage} />
                                </div>
                            </td>
                            <td className="muted">{c.email}</td>
                            <td className="muted nowrap">{formatDate(c.createdAt)}</td>
                        </tr>
                        <tr className="row-detail">
                            <td colSpan={3}>
                                <CommentNode comment={c} isRoot {...shared} />
                            </td>
                        </tr>
                    </Fragment>
                ))}
                </tbody>
            </table>
        </div>
    )
}