export type AttachmentDto = {
    id: number
    kind: 'Image' | 'Text'
    status: 'Processing' | 'Ready' | 'Failed'
    originalFileName: string
    url: string
    width: number | null
    height: number | null
}

export type CommentDto = {
    id: number
    parentId: number | null
    userName: string
    email: string
    homePage: string | null
    text: string
    createdAt: string
    attachment: AttachmentDto | null
    replies: CommentDto[]
}

export type PagedResult<T> = {
    items: T[]
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
}

export type SortField = 'createdAt' | 'userName' | 'email'
export type SortDir = 'asc' | 'desc'