/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions.base;

import java.util.ArrayList;
import java.util.Iterator;

/**
 * AnnounceConditionList class. Wrapper class of system collection used to hold and access AnnounceConditions instances.
 * @author Dimitar Karamanov
 */
public final class AnnounceConditionList {

    /**
     * Internal container collection.
     */
    private final ArrayList<AnnounceCondition> collection = new ArrayList<AnnounceCondition>();

    /**
     * Constructor.
     */
    public AnnounceConditionList() {
        super();
    }

    /**
     * Clears all elements from the collection.
     */
    public void clear() {
        collection.clear();
    }

    /**
     * Adds a condition to the collection.
     * @param condition to be added
     */
    public void add(AnnounceCondition condition) {
        collection.add(condition);
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
    public AnnounceConditionIterator iterator() {
        return new AnnounceConditionIteratorImpl(collection.iterator());
    }

    /**
     * PlayableIteratorImpl class. Implements PlayCardMethodIterator interface.
     */
    private class AnnounceConditionIteratorImpl implements AnnounceConditionIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<AnnounceCondition> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public AnnounceConditionIteratorImpl(Iterator<AnnounceCondition> enumeration) {
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
        public AnnounceCondition next() {
            return (AnnounceCondition) enumeration.next();
        }
    }

}
