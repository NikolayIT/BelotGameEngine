/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.framework.message;

import java.util.ArrayList;

/**
 * MessageQueue class.
 * @author Dimitar Karamanov
 */
public final class MessageQueue {

    /**
     * Internal container object.
     */
    private final ArrayList<Message> messageList = new ArrayList<Message>();

    /**
     * Constructor.
     */
    public MessageQueue() {
        super();
    }

    /**
     * Adds message to the list.
     * @param message new message.
     */
    protected final void addMessage(final Message message) {
        synchronized (messageList) {
            if (message != null) {
                messageList.add(message);
                messageList.notify();
            }
        }
    }

    /**
     * Returns one message from the queue.
     * @return Message extracted from queue.
     */
    protected final Message getMessage() {
        synchronized (messageList) {
            while (messageList.size() == 0) {
                try {
                    messageList.wait();
                } catch (Exception e) {
                }
            }
            if (messageList.size() > 0) {
                final Message message = (Message) messageList.get(0);
                messageList.remove(message);
                return message;
            }
        }
        return null;
    }

    /**
     * Returns true if there are messages int the queue false otherwise.
     * @return boolean true if there are messages int the queue false otherwise.
     */
    public final boolean hasMessage() {
        return messageList.size() > 0;
    }

    public final void clearAll() {
        messageList.clear();
    }
}
