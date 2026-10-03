import { useCallback, useEffect, useId, useRef, useState } from 'react'
import type { ChangeEvent, FormEvent, ReactNode } from 'react'
import { createComment, getCaptcha, previewText } from '../api/client'
import type { FieldErrors } from '../api/client'
import type { CommentDto } from '../api/types'
import { MAX_TEXT_LENGTH, isHttpUrl, validateFile, validateForm } from '../utils/validation'
import type { FormValues } from '../utils/validation'

type Props = {
    parentId?: number
    onSuccess: (comment: CommentDto) => void
    onCancel?: () => void
}

type Captcha = { id: string; image: string }
type Preview = { html: string } | { error: string }
type TagName = 'i' | 'strong' | 'code' | 'a'

const AUTHOR_KEY = 'comments.author'
const TAGS: TagName[] = ['i', 'strong', 'code', 'a']

function loadAuthor(): Pick<FormValues, 'userName' | 'email' | 'homePage'> {
    const empty = { userName: '', email: '', homePage: '' }
    try {
        const raw = localStorage.getItem(AUTHOR_KEY)
        return raw ? { ...empty, ...JSON.parse(raw) } : empty
    } catch {
        return empty
    }
}

export function CommentForm({ parentId, onSuccess, onCancel }: Props) {
    const id = useId()
    const [values, setValues] = useState<FormValues>(() => ({ ...loadAuthor(), text: '', captchaCode: '' }))
    const [file, setFile] = useState<File | null>(null)
    const [errors, setErrors] = useState<FieldErrors>({})
    const [captcha, setCaptcha] = useState<Captcha | null>(null)
    const [showPreview, setShowPreview] = useState(false)
    const [preview, setPreview] = useState<Preview | null>(null)
    const [submitting, setSubmitting] = useState(false)
    const textRef = useRef<HTMLTextAreaElement>(null)
    const fileRef = useRef<HTMLInputElement>(null)

    const refreshCaptcha = useCallback(async () => {
        try {
            setCaptcha(await getCaptcha())
        } catch {
            setErrors((prev) => ({ ...prev, captchaCode: 'Could not load CAPTCHA.' }))
        }
    }, [])

    useEffect(() => {
        void refreshCaptcha()
    }, [refreshCaptcha])

    // live preview through the server sanitizer, without page reload
    useEffect(() => {
        if (!showPreview) return
        const timer = setTimeout(async () => {
            if (!values.text.trim()) {
                setPreview({ html: '' })
                return
            }
            try {
                setPreview(await previewText(values.text))
            } catch {
                setPreview({ error: 'Preview failed.' })
            }
        }, 300)
        return () => clearTimeout(timer)
    }, [showPreview, values.text])

    const clearError = (field: string) =>
        setErrors((prev) => {
            if (!prev[field]) return prev
            const next = { ...prev }
            delete next[field]
            return next
        })

    const update = (field: keyof FormValues, value: string) => {
        setValues((prev) => ({ ...prev, [field]: value }))
        clearError(field)
    }

    const wrapSelection = (tag: TagName) => {
        const el = textRef.current
        if (!el) return

        const { selectionStart: start, selectionEnd: end, value } = el
        const selected = value.slice(start, end)
        let openTag = `<${tag}>`

        if (tag === 'a') {
            const url = window.prompt('Link URL (http or https):', 'https://')
            if (!url) return
            if (!isHttpUrl(url.trim())) {
                setErrors((prev) => ({ ...prev, text: 'Link must be a valid http(s) URL.' }))
                return
            }
            const title = window.prompt('Link title (optional):', '') ?? ''
            const escape = (s: string) => s.replace(/"/g, '&quot;')
            openTag = title.trim()
                ? `<a href="${escape(url.trim())}" title="${escape(title.trim())}">`
                : `<a href="${escape(url.trim())}">`
        }

        const next = value.slice(0, start) + openTag + selected + `</${tag}>` + value.slice(end)
        update('text', next)

        requestAnimationFrame(() => {
            el.focus()
            const cursor = start + openTag.length
            el.setSelectionRange(cursor, cursor + selected.length)
        })
    }

    const handleFile = (e: ChangeEvent<HTMLInputElement>) => {
        const selected = e.target.files?.[0] ?? null
        setFile(selected)
        const fileError = validateFile(selected)
        setErrors((prev) => {
            const next = { ...prev }
            if (fileError) next.file = fileError
            else delete next.file
            return next
        })
    }

    const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
        e.preventDefault()

        const clientErrors = validateForm(values, file)
        setErrors(clientErrors)
        if (Object.keys(clientErrors).length > 0 || !captcha) return

        const form = new FormData()
        form.append('userName', values.userName.trim())
        form.append('email', values.email.trim())
        if (values.homePage.trim()) form.append('homePage', values.homePage.trim())
        form.append('text', values.text)
        form.append('captchaId', captcha.id)
        form.append('captchaCode', values.captchaCode.trim())
        if (parentId !== undefined) form.append('parentId', String(parentId))
        if (file) form.append('file', file)

        setSubmitting(true)
        try {
            const result = await createComment(form)
            if (result.ok) {
                localStorage.setItem(
                    AUTHOR_KEY,
                    JSON.stringify({ userName: values.userName, email: values.email, homePage: values.homePage }),
                )
                setValues((prev) => ({ ...prev, text: '', captchaCode: '' }))
                setFile(null)
                if (fileRef.current) fileRef.current.value = ''
                setShowPreview(false)
                onSuccess(result.comment)
            } else {
                setErrors(result.errors)
            }
        } catch {
            setErrors({ form: 'Network error. Please try again.' })
        } finally {
            setSubmitting(false)
            setValues((prev) => ({ ...prev, captchaCode: '' }))
            void refreshCaptcha() // a CAPTCHA can be used only once
        }
    }

    return (
        <form className="card comment-form" onSubmit={handleSubmit} noValidate>
            <div className="form-grid">
                <Field id={`${id}-name`} label="User Name" required error={errors.userName}>
                    <input
                        id={`${id}-name`}
                        value={values.userName}
                        onChange={(e) => update('userName', e.target.value)}
                        maxLength={50}
                        autoComplete="username"
                        placeholder="Letters and digits"
                    />
                </Field>
                <Field id={`${id}-email`} label="E-mail" required error={errors.email}>
                    <input
                        id={`${id}-email`}
                        type="email"
                        value={values.email}
                        onChange={(e) => update('email', e.target.value)}
                        maxLength={254}
                        autoComplete="email"
                        placeholder="you@example.com"
                    />
                </Field>
                <Field id={`${id}-home`} label="Home page" error={errors.homePage}>
                    <input
                        id={`${id}-home`}
                        type="url"
                        value={values.homePage}
                        onChange={(e) => update('homePage', e.target.value)}
                        maxLength={2048}
                        placeholder="https://..."
                    />
                </Field>
            </div>

            <Field id={`${id}-text`} label="Text" required error={errors.text}>
                <div className="toolbar" role="toolbar" aria-label="Formatting">
                    {TAGS.map((tag) => (
                        <button key={tag} type="button" onClick={() => wrapSelection(tag)} title={`Wrap selection in <${tag}>`}>
                            [{tag}]
                        </button>
                    ))}
                </div>
                <textarea
                    id={`${id}-text`}
                    ref={textRef}
                    value={values.text}
                    onChange={(e) => update('text', e.target.value)}
                    maxLength={MAX_TEXT_LENGTH}
                    placeholder={'Allowed tags: <a href="" title="">, <code>, <i>, <strong>'}
                />
                <span className="muted small">{values.text.length}/{MAX_TEXT_LENGTH}</span>
            </Field>

            {showPreview && preview && (
                <div className="preview">
                    <div className="preview-title">Preview</div>
                    {'error' in preview ? (
                        <span className="field-error">{preview.error}</span>
                    ) : preview.html ? (
                        // html comes from the server sanitizer, so it's safe to render
                        <div className="comment-text" dangerouslySetInnerHTML={{ __html: preview.html }} />
                    ) : (
                        <span className="muted">Nothing to preview yet.</span>
                    )}
                </div>
            )}

            <Field id={`${id}-file`} label="Attachment: JPG, PNG, GIF (shrunk to 320×240) or TXT up to 100 KB" error={errors.file}>
                <input id={`${id}-file`} ref={fileRef} type="file" accept=".jpg,.jpeg,.png,.gif,.txt" onChange={handleFile} />
            </Field>

            <Field id={`${id}-captcha`} label="CAPTCHA" required error={errors.captchaCode}>
                <div className="captcha-row">
                    {captcha ? <img src={captcha.image} alt="CAPTCHA" /> : <span className="spinner" />}
                    <button type="button" className="link-btn" onClick={() => void refreshCaptcha()}>
                        ↻ New code
                    </button>
                    <input
                        id={`${id}-captcha`}
                        value={values.captchaCode}
                        onChange={(e) => update('captchaCode', e.target.value)}
                        maxLength={10}
                        autoComplete="off"
                        placeholder="Code"
                    />
                </div>
            </Field>

            {(errors.form || errors.parentId) && <div className="alert">{errors.form ?? errors.parentId}</div>}

            <div className="form-actions">
                <button type="submit" className="btn" disabled={submitting}>
                    {submitting ? 'Sending…' : parentId !== undefined ? 'Reply' : 'Publish'}
                </button>
                <button type="button" className="btn btn--ghost" onClick={() => setShowPreview((p) => !p)}>
                    {showPreview ? 'Hide preview' : 'Preview'}
                </button>
                {onCancel && (
                    <button type="button" className="link-btn" onClick={onCancel}>
                        Cancel
                    </button>
                )}
            </div>
        </form>
    )
}

type FieldProps = { id: string; label: string; required?: boolean; error?: string; children: ReactNode }

function Field({ id, label, required, error, children }: FieldProps) {
    return (
        <div className={`field${error ? ' field--error' : ''}`}>
            <label htmlFor={id}>
                {label}
                {required && <span className="required"> *</span>}
            </label>
            {children}
            {error && <span className="field-error">{error}</span>}
        </div>
    )
}