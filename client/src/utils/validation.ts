import type { FieldErrors } from '../api/client'

export type FormValues = {
    userName: string
    email: string
    homePage: string
    text: string
    captchaCode: string
}

export const MAX_TEXT_LENGTH = 5000
const MAX_TXT_BYTES = 100 * 1024
const MAX_IMAGE_BYTES = 5 * 1024 * 1024

const LATIN_AND_DIGITS = /^[A-Za-z0-9]+$/
const EMAIL = /^[^@\s]+@[^@\s]+\.[^@\s]+$/
const ALLOWED_TAGS = new Set(['a', 'code', 'i', 'strong'])
const TAG = /<(\/?)([a-zA-Z][a-zA-Z0-9]*)([^<>]*)>/g
const LINK_ATTRIBUTES = /^(\s+(href|title)\s*=\s*("[^"]*"|'[^']*'))*\s*$/i
const HREF = /\shref\s*=\s*(?:"([^"]*)"|'([^']*)')/i

export function isHttpUrl(value: string): boolean {
    try {
        const url = new URL(value)
        return url.protocol === 'http:' || url.protocol === 'https:'
    } catch {
        return false
    }
}

/** Same rules as the server sanitizer: only a/code/i/strong, properly nested and closed. */
export function validateMarkup(text: string): string | null {
    const open: string[] = []

    for (const match of text.matchAll(TAG)) {
        const closing = match[1] === '/'
        const name = match[2].toLowerCase()
        const attributes = match[3]

        if (!ALLOWED_TAGS.has(name)) return `Tag <${name}> is not allowed. Allowed: <a>, <code>, <i>, <strong>.`

        if (closing) {
            const expected = open.pop()
            if (expected !== name) {
                return expected
                    ? `Expected </${expected}> but found </${name}>.`
                    : `Closing tag </${name}> has no opening tag.`
            }
            continue
        }

        if (attributes.trim().endsWith('/')) return `Self-closing <${name} /> is not allowed.`

        if (name !== 'a') {
            if (attributes.trim()) return `Tag <${name}> cannot have attributes.`
        } else {
            if (!LINK_ATTRIBUTES.test(attributes)) return 'Invalid attributes in <a>. Use: <a href="https://..." title="...">.'
            const href = HREF.exec(attributes)
            const url = href?.[1] ?? href?.[2]
            if (!url || !isHttpUrl(url.trim())) return 'Tag <a> requires href with a valid http(s) URL.'
        }

        open.push(name)
    }

    return open.length > 0 ? `Tag <${open[open.length - 1]}> is not closed.` : null
}

export function validateFile(file: File | null): string | null {
    if (!file) return null
    const dot = file.name.lastIndexOf('.')
    const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : ''

    if (extension === '.txt') return file.size > MAX_TXT_BYTES ? 'Text file must not exceed 100 KB.' : null
    if (['.jpg', '.jpeg', '.png', '.gif'].includes(extension)) {
        return file.size > MAX_IMAGE_BYTES ? 'Image must not exceed 5 MB.' : null
    }
    return 'Allowed files: JPG, GIF, PNG images or a TXT file up to 100 KB.'
}

export function validateForm(values: FormValues, file: File | null): FieldErrors {
    const errors: FieldErrors = {}

    if (!values.userName.trim()) errors.userName = 'User name is required.'
    else if (values.userName.length > 50 || !LATIN_AND_DIGITS.test(values.userName))
        errors.userName = 'Only Latin letters and digits, up to 50 characters.'

    if (!values.email.trim()) errors.email = 'E-mail is required.'
    else if (values.email.length > 254 || !EMAIL.test(values.email)) errors.email = 'Invalid e-mail format.'

    if (values.homePage.trim() && !isHttpUrl(values.homePage.trim()))
        errors.homePage = 'Home page must be a valid http(s) URL.'

    if (!values.text.trim()) errors.text = 'Text is required.'
    else if (values.text.length > MAX_TEXT_LENGTH) errors.text = `Text must not exceed ${MAX_TEXT_LENGTH} characters.`
    else {
        const markupError = validateMarkup(values.text)
        if (markupError) errors.text = markupError
    }

    if (!values.captchaCode.trim()) errors.captchaCode = 'Enter the code from the picture.'
    else if (!LATIN_AND_DIGITS.test(values.captchaCode.trim()))
        errors.captchaCode = 'The code contains only Latin letters and digits.'

    const fileError = validateFile(file)
    if (fileError) errors.file = fileError

    return errors
}