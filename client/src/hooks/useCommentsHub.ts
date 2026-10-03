import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'

export type HubEvent =
    | { type: 'commentCreated'; commentId: number }
    | { type: 'attachmentUpdated'; commentId: number }

/** Connects to the SignalR hub and reports server events. Returns connection status. */
export function useCommentsHub(onEvent: (event: HubEvent) => void): boolean {
    const handlerRef = useRef(onEvent)
    const [connected, setConnected] = useState(false)

    useEffect(() => {
        handlerRef.current = onEvent
    }, [onEvent])

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl('/hubs/comments')
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Warning)
            .build()

        connection.on('commentCreated', (comment: { id: number }) =>
            handlerRef.current({ type: 'commentCreated', commentId: comment.id }))
        connection.on('attachmentUpdated', (payload: { commentId: number }) =>
            handlerRef.current({ type: 'attachmentUpdated', commentId: payload.commentId }))

        connection.onreconnecting(() => setConnected(false))
        connection.onreconnected(() => setConnected(true))
        connection.onclose(() => setConnected(false))

        connection
            .start()
            .then(() => setConnected(connection.state === HubConnectionState.Connected))
            .catch(() => setConnected(false))

        return () => {
            void connection.stop()
        }
    }, [])

    return connected
}
