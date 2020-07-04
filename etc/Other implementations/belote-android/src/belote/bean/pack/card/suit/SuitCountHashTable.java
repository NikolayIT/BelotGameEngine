/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card.suit;

import java.io.Serializable;
import java.util.Enumeration;
import java.util.Hashtable;

import belote.base.IntegerIterator;

/**
 * SuitCountHashTable class.
 * @author Dimitar Karamanov
 */
public final class SuitCountHashTable implements Serializable {

    /**
	 * Serial versionUID.
	 */
    private static final long serialVersionUID = 1804382243836131584L;

    /**
     * Internal system collection object.
     */
    private final Hashtable<Suit, Integer> hashTable = new Hashtable<Suit, Integer>();

    /**
     * Constructor.
     */
    public SuitCountHashTable() {
        super();
    }

    /**
     * Puts an element with provided key and count.
     * @param suit provided suit key.
     * @param count associated key count value.
     */
    public void put(final Suit suit, final Integer count) {
        if (contains(suit)) {
            remove(suit);
        }
        hashTable.put(suit, count);
    }

    /**
     * Removes element by key suit.
     * @param suit elements key.
     */
    public void remove(final Suit suit) {
        hashTable.remove(suit);
    }

    /**
     * Returns true if the Hashtable contains element with provided key, false otherwise.
     * @param suit element key.
     * @return true if the Hashtable contains element with provided key, false otherwise.
     */
    public boolean contains(final Suit suit) {
        return hashTable.containsKey(suit);
    }

    /**
     * Returns collection size.
     * @return collection size.
     */
    public int size() {
        return hashTable.size();
    }

    /**
     * Clears the collection.
     */
    public void clear() {
        hashTable.clear();
    }

    /**
     * Returns iterator for the list.
     * @return SuitIterator iterator.
     */
    public SuitIterator suitIterator() {
        return new SuitIteratorImpl(hashTable.keys());
    }

    /**
     * Returns iterator for the collection.
     * @return IntegerIterator iterator.
     */
    public IntegerIterator countIterator() {
        return new IntegerIteratorImpl(hashTable.elements());
    }

    /**
     * SuitIteratorImpl class. Implements SuitIterator interface.
     */
    private class SuitIteratorImpl implements SuitIterator {

        /**
         * The internal collection enumerator.
         */
        private final Enumeration<Suit> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public SuitIteratorImpl(final Enumeration<Suit> enumeration) {
            this.enumeration = enumeration;
        }

        /**
         * Returns true if the iteration has more elements.
         * @return boolean true if the iteration has more elements false otherwise.
         */
        public boolean hasNext() {
            return enumeration.hasMoreElements();
        }

        /**
         * Returns the next element in the iteration.
         * @return Suit the next element in the iteration.
         */
        public Suit next() {
            return enumeration.nextElement();
        }
    }

    /**
     * IntegerIteratorImpl class. Implements IntegerIterator interface.
     */
    private class IntegerIteratorImpl implements IntegerIterator {

        /**
         * The internal collection enumerator.
         */
        private final Enumeration<Integer> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public IntegerIteratorImpl(final Enumeration<Integer> enumeration) {
            this.enumeration = enumeration;
        }

        /**
         * Returns true if the iteration has more elements.
         * @return boolean true if the iteration has more elements false otherwise.
         */
        public boolean hasNext() {
            return enumeration.hasMoreElements();
        }

        /**
         * Returns the next element in the iteration.
         * @return Integer the next element in the iteration.
         */
        public Integer next() {
            return enumeration.nextElement();
        }
    }
}
