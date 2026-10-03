import type { CommentDto, PagedResult, SortDir, SortField } from './types'

export type FieldErrors = Record<string, string>

export async function getComments(page: number, sortBy: SortField, sortDir: SortDir): Promise<PagedResult<CommentDto>> {
    const params = new URLSearchParams({ page: String(page), sortBy, sortDir })
    const res = await fetch(`/api/comments?${params}`)
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    return res.json()
}

export async function getCaptcha(): Promise<{ id: string; image: string }> {
    const res = await fetch('/api/captcha', { cache: 'no-store' })
    if (!res.ok) throw new Error(`HTTP ${res.status}`)
    return res.json()
}

export async function previewText(text: string): Promise<{ html: string } | { error: string }> {
    const res = await fetch('/api/comments/preview', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text }),
    })
    if (res.ok) return res.json()
    const errors = await readErrors(res)
    return { error: errors.text ?? errors.form ?? 'Preview failed.' }
}

export async function createComment(
    form: FormData,
): Promise<{ ok: true; comment: CommentDto } | { ok: false; errors: FieldErrors }> {
    const res = await fetch('/api/comments', { method: 'POST', body: form })
    if (res.ok) return { ok: true, comment: await res.json() }
    return { ok: false, errors: await readErrors(res) }
}

async function readErrors(res: Response): Promise<FieldErrors> {
    if (res.status === 413) return { file: 'File is too large.' }
    try {
        const body = (await res.json()) as { errors?: Record<string, string[]> }
        const result: FieldErrors = {}
        for (const [key, messages] of Object.entries(body.errors ?? {})) {
            const field = key.charAt(0).toLowerCase() + key.slice(1)
            result[field] = messages[0]
        }
        return Object.keys(result).length > 0 ? result : { form: `Request failed (${res.status}).` }
    } catch {
        return { form: `Request failed (${res.status}).` }
    }
}