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

/**
 * Message class.
 * @author Dimitar Karamanov
 */
public class Message {

    /**
     * Message data.
     */
    protected final Object data;

    /**
     * Message type.
     */
    protected final MessageType messageType;

    /**
     * Constructor.
     * @param messageType message type.
     */
    public Message(final MessageType messageType) {
        this(messageType, null);
    }

    /**
     * Constructor.
     * @param messageType message type.
     * @param data message data.
     */
    public Message(final MessageType messageType, final Object data) {
        this.messageType = messageType;
        this.data = data;
    }

    /**
     * Returns message data.
     * @return Object message data.
     */
    public final Object getData() {
        return data;
    }

    /**
     * Returns message type.
     * @return MessageType message type.
     */
    public final MessageType getMessageType() {
        return messageType;
    }
}
