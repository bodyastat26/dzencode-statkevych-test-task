import { useEffect, useRef, useState } from 'react'

type Props = { url: string; title: string; onClose: () => void }

export function TextFileModal({ url, title, onClose }: Props) {
    const [text, setText] = useState<string | null>(null)
    const [failed, setFailed] = useState(false)
    const closeRef = useRef(onClose)

    useEffect(() => {
        closeRef.current = onClose
    }, [onClose])

    useEffect(() => {
        const controller = new AbortController()
        fetch(url, { signal: controller.signal })
            .then((res) => (res.ok ? res.text() : Promise.reject(new Error(`HTTP ${res.status}`))))
            .then(setText)
            .catch(() => {
                if (!controller.signal.aborted) setFailed(true)
            })

        const onKey = (e: KeyboardEvent) => {
            if (e.key === 'Escape') closeRef.current()
        }
        window.addEventListener('keydown', onKey)

        return () => {
            controller.abort()
            window.removeEventListener('keydown', onKey)
        }
    }, [url])

    return (
        <div className="modal-backdrop" onClick={onClose}>
            <div className="modal" role="dialog" aria-modal="true" onClick={(e) => e.stopPropagation()}>
                <div className="modal-header">
                    <span>{title}</span>
                    <button type="button" className="icon-btn" onClick={onClose} aria-label="Close">×</button>
                </div>
                {/* rendered as text, never as HTML */}
                <pre className="modal-body">{failed ? 'Could not load the file.' : (text ?? 'Loading…')}</pre>
            </div>
        </div>
    )
}