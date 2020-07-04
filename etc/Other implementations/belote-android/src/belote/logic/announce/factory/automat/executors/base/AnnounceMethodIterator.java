/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.executors.base;

import belote.logic.announce.factory.automat.base.AnnounceMethod;

/**
 * AnnounceMethodIterator interface.
 * @author Dimitar Karamanov
 */
public interface AnnounceMethodIterator {

    /**
     * Returns true if the iteration has more elements.
     * @return boolean true if the iteration has more elements false otherwise.
     */
    boolean hasNext();

    /**
     * Returns the next element in the iteration.
     * @return AnnounceMethod the next element in the iteration.
     */
    AnnounceMethod next();
}