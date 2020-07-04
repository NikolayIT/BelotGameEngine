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
import java.util.ArrayList;
import java.util.Iterator;

/**
 * SuitList class. Wrapper class of system collection used to hold and access Suit instances.
 * @author Dimitar Karamanov
 */
public final class SuitList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -7015889465999912329L;
    
    /**
     * Internal container collection.
     */
    private ArrayList<Suit> collection = new ArrayList<Suit>();

    /**
     * Constructor.
     */
    public SuitList() {
        super();
    }

    /**
     * Clears all elements from the list.
     */
    public void clear() {
        collection.clear();
    }

    /**
     * Adds a suit to the list.
     * @param suit provided suit.
     */
    public void add(final Suit suit) {
        if (!contains(suit)) {
            collection.add(suit);
        }
    }

    /**
     * Returns suit list size.
     * @return int size of the list.
     */
    public int size() {
        return collection.size();
    }

    /**
     * Returns true if the list contains provided suit and false otherwise.
     * @param suit provided suit.
     * @return boolean true if the list contains provided suit and false otherwise.
     */
    public boolean contains(Suit suit) {
        return collection.contains(suit);
    }

    /**
     * Returns iterator for the list.
     * @return SuitIterator iterator.
     */
    public SuitIterator iterator() {
        return new SuitIteratorImpl(collection.iterator());
    }

    /**
     * Returns provided suit index.
     * @param suit provided suit.
     * @return int index of the specified suit.
     */
    public int getSuitIndex(Suit suit) {
        return collection.indexOf(suit);
    }

    /**
     * Returns a HTML representation of the object.
     * @return String a HTML representation of the object.
     */
    public String toHTMLString() {
        final StringBuffer sb = new StringBuffer();
        for (final SuitIterator iterator = iterator(); iterator.hasNext();) {
            if (sb.length() != 0) {
                sb.append(",");
            }
            final Suit suit = iterator.next();
            sb.append(suit.getSuitImageTag());
        }
        return sb.toString();
    }

    /**
     * SuitIteratorImpl class. Implements SuitIterator interface.
     */
    private class SuitIteratorImpl implements SuitIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Suit> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public SuitIteratorImpl(final Iterator<Suit> enumeration) {
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
         * @return Suit the next element in the iteration.
         */
        public Suit next() {
            return enumeration.next();
        }
    }
}
