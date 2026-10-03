type Props = { page: number; totalPages: number; onChange: (page: number) => void }

export function Pagination({ page, totalPages, onChange }: Props) {
    if (totalPages <= 1) return null

    const start = Math.max(1, page - 2)
    const end = Math.min(totalPages, page + 2)
    const pages = Array.from({ length: end - start + 1 }, (_, i) => start + i)

    return (
        <nav className="pagination" aria-label="Pages">
            <button type="button" disabled={page === 1} onClick={() => onChange(page - 1)}>‹</button>
            {start > 1 && (
                <>
                    <button type="button" onClick={() => onChange(1)}>1</button>
                    {start > 2 && <span className="ellipsis">…</span>}
                </>
            )}
            {pages.map((p) => (
                <button key={p} type="button" className={p === page ? 'active' : ''} onClick={() => onChange(p)}>
                    {p}
                </button>
            ))}
            {end < totalPages && (
                <>
                    {end < totalPages - 1 && <span className="ellipsis">…</span>}
                    <button type="button" onClick={() => onChange(totalPages)}>{totalPages}</button>
                </>
            )}
            <button type="button" disabled={page === totalPages} onClick={() => onChange(page + 1)}>›</button>
        </nav>
    )
}