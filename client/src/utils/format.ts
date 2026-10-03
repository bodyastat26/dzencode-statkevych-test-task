export function formatDate(iso: string): string {
    const d = new Date(iso)
    const pad = (n: number) => n.toString().padStart(2, '0')
    return `${pad(d.getDate())}.${pad(d.getMonth() + 1)}.${d.getFullYear().toString().slice(2)} at ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

export function avatarColor(name: string): string {
    let hash = 0
    for (const ch of name) hash = (hash * 31 + ch.charCodeAt(0)) | 0
    return `hsl(${Math.abs(hash) % 360} 55% 55%)`
}