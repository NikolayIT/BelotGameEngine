/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.base.executor;

import java.util.ArrayList;
import java.util.Iterator;

import belote.logic.play.strategy.automat.base.PlayCardMethod;


/**
 * PlayCardMethodList class. Wrapper class of system collection used to hold and access PlayCardMethod instances.
 * @author Dimitar Karamanov
 */
public final class PlayCardMethodList {

    /**
     * Internal container collection.
     */
    private ArrayList<PlayCardMethod> collection = new ArrayList<PlayCardMethod>();

    /**
     * Constructor.
     */
    public PlayCardMethodList() {
        super();
    }

    /**
     * Clears all elements from the collection.
     */
    public void clear() {
        collection.clear();
    }

    /**
     * Adds a method to the collection.
     * @param method to be added
     */
    public void add(PlayCardMethod method) {
        collection.add(method);
    }

    /**
     * Returns collection size.
     * @return int size of the method's collection
     */
    public int size() {
        return collection.size();
    }

    /**
     * Returns iterator for the collection.
     * @return PlayCardMethodIterator iterator.
     */
    public PlayCardMethodIterator iterator() {
        return new PlayCardMethodIteratorImpl(collection.iterator());
    }

    /**
     * PlayableIteratorImpl class. Implements PlayCardMethodIterator interface.
     */
    private class PlayCardMethodIteratorImpl implements PlayCardMethodIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<PlayCardMethod> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public PlayCardMethodIteratorImpl(final Iterator<PlayCardMethod> enumeration) {
            this.enumeration = enumeration;
        }

        /**
         * Returns true if the iteration has more elements.
         * @return boolean true if the iteration has more elements false otherwise.
         */
        public boolean hasNext() {
            return enumeration.hasNext();
        }

        /**
         * Returns the next element in the iteration.
         * @return Playable the next element in the iteration.
         */
        public PlayCardMethod next() {
            return enumeration.next();
        }
    }
}
