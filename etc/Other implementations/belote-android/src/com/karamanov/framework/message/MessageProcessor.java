package com.karamanov.framework.message;

import java.util.Hashtable;

public final class MessageProcessor implements Processor {

    private final MessageThread messageThread;

    private final MessageQueue messageQueue;

    private boolean processMessages = true;
    
    /**
     * Hash table which maps messages with handlers.
     */
    private final Hashtable<MessageType, Messageable> listenersHash = new Hashtable<MessageType, Messageable>();

    public MessageProcessor() {
        messageQueue = new MessageQueue();
        messageThread = new MessageThread(this);
    }

    public final void start() {
        messageThread.start();
    }
    
    /**
     * Sends message.
     * @param message - to be send.
     */
    public final void sendMessage(Message message, boolean always) {
        if (processMessages || always) {
            messageQueue.addMessage(message);
        }
    }

    /**
     * Sends message.
     * @param message - to be send.
     */
    public final void sendMessage(Message message) {
        sendMessage(message, false);
    }

    public final void stopMessaging() {
        messageQueue.clearAll();
        processMessages = false;
    }

    public final void runMessaging() {
        processMessages = true;
    }
    
    /**
     * Adds message listener for the concrete message type.
     * @param messageType concrete message type.
     * @param messageable message listener.
     */
    public final void addMessageListener(final MessageType messageType, final Messageable messageable) {
        if (listenersHash.containsKey(messageType)) {
            listenersHash.remove(messageType);
        }
        listenersHash.put(messageType, messageable);
    }

    /**
     * Removes message listener for the concrete message type.
     * @param messageType concrete message type.
     */
    public final void removeMessageListener(final MessageType messageType) {
        listenersHash.remove(messageType);
    }
    
    /**
     * Process one message.
     */
    public final void process() {
        final Message message = messageQueue.getMessage();
        if (message != null) {
            final Messageable messageable = (Messageable) listenersHash.get(message.getMessageType());
            if (messageable != null) {
                messageable.performMessage(message);
            }
        }
    }
}
