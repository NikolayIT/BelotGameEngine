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

import java.util.ArrayList;
import java.util.Iterator;

import belote.logic.announce.factory.automat.base.AnnounceMethod;

/**
 * AnnounceMethodList class. Wrapper class of system collection used to hold and access AnnounceMethod instances.
 * @author Dimitar Karamanov
 */
public final class AnnounceMethodList {

    /**
     * Internal container collection.
     */
    private final ArrayList<AnnounceMethod> collection = new ArrayList<AnnounceMethod>();

    /**
     * Constructor.
     */
    public AnnounceMethodList() {
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
    public void add(AnnounceMethod method) {
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
     * @return AnnounceMethodIterator iterator.
     */
    public AnnounceMethodIterator iterator() {
        return new AnnounceMethodIteratorImpl(collection.iterator());
    }

    /**
     * AnnounceMethodIteratorImpl class. Implements AnnounceMethodIterator interface.
     */
    private class AnnounceMethodIteratorImpl implements AnnounceMethodIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<AnnounceMethod> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public AnnounceMethodIteratorImpl(final Iterator<AnnounceMethod> enumeration) {
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
         * AnnounceMethod the next element in the iteration.
         * @return AnnounceMethod the next element in the iteration.
         */
        public AnnounceMethod next() {
            return enumeration.next();
        }
    }
}
