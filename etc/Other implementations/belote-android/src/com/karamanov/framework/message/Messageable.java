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
 * Messageable interface.
 * @author Dimitar Karamanov
 */
public interface Messageable {

    /**
     * Performs message.
     * @param message performing message.
     */
    void performMessage(Message message);
}
