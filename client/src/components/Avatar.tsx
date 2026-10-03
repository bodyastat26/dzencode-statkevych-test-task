import { avatarColor } from '../utils/format'

export function Avatar({ name }: { name: string }) {
    return (
        <div className="avatar" style={{ background: avatarColor(name) }} aria-hidden="true">
            {name.slice(0, 1).toUpperCase()}
        </div>
    )
}

export function AuthorName({ name, homePage }: { name: string; homePage: string | null }) {
    // homePage is validated on the server to be an http(s) URL, so it can't be a javascript: link
    return homePage ? (
        <a className="author" href={homePage} target="_blank" rel="nofollow noopener noreferrer">{name}</a>
    ) : (
        <span className="author">{name}</span>
    )
}