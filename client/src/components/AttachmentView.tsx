import { useState } from 'react'
import Lightbox from 'yet-another-react-lightbox'
import 'yet-another-react-lightbox/styles.css'
import type { AttachmentDto } from '../api/types'
import { TextFileModal } from './TextFileModal'

export function AttachmentView({ attachment }: { attachment: AttachmentDto }) {
    const [open, setOpen] = useState(false)

    if (attachment.status === 'Processing') {
        return (
            <div className="attachment attachment--processing">
                <span className="spinner" /> Processing image…
            </div>
        )
    }

    if (attachment.status === 'Failed') {
        return <div className="attachment attachment--failed">Could not process {attachment.originalFileName}</div>
    }

    if (attachment.kind === 'Image') {
        return (
            <>
                <button type="button" className="attachment-thumb" onClick={() => setOpen(true)} title="Click to enlarge">
                    <img
                        src={attachment.url}
                        alt={attachment.originalFileName}
                        width={attachment.width ?? undefined}
                        height={attachment.height ?? undefined}
                        loading="lazy"
                    />
                </button>
                <Lightbox
                    open={open}
                    close={() => setOpen(false)}
                    slides={[{ src: attachment.url, alt: attachment.originalFileName }]}
                    carousel={{ finite: true }}
                    render={{ buttonPrev: () => null, buttonNext: () => null }}
                    animation={{ fade: 300 }}
                />
            </>
        )
    }

    return (
        <>
            <button type="button" className="attachment-file" onClick={() => setOpen(true)}>
                📄 {attachment.originalFileName}
            </button>
            {open && <TextFileModal url={attachment.url} title={attachment.originalFileName} onClose={() => setOpen(false)} />}
        </>
    )
}