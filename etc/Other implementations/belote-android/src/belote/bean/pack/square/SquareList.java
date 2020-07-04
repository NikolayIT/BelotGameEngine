/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.square;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;

/**
 * SquareList class. Wrapper class of system collection used to hold and access Square instances.
 * @author Dimitar Karamanov
 */
public final class SquareList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 2716681164194070603L;
    
    /**
     * Internal container collection.
     */
    private ArrayList<Square> collection = new ArrayList<Square>();

    /**
     * Appends Square element to the end of the collection.
     * @param square - element to be appended to this list.
     */
    public void add(final Square square) {
        collection.add(square);
    }

    /**
     * Removes all of the elements from the collection.
     */
    public void clear() {
        collection.clear();
    }

    /**
     * Returns whether the collection is empty.
     * @return boolean true if the collection is empty false otherwise.
     */
    public boolean isEmpty() {
        return collection.isEmpty();
    }

    /**
     * Returns an iterator over the elements in the collection.
     * @return SquareIterator iterator.
     */
    public SquareIterator iterator() {
        return new SquareIteratorImpl(collection.iterator());
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        final StringBuffer sb = new StringBuffer();

        for (final SquareIterator iterator = iterator(); iterator.hasNext();) {
            if (sb.length() != 0) {
                sb.append(" ");
            }
            sb.append(iterator.next().toString());
        }
        return sb.toString();
    }

    /**
     * SquareIteratorImpl class. Implements SquareIterator interface.
     */
    private class SquareIteratorImpl implements SquareIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Square> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public SquareIteratorImpl(final Iterator<Square> enumeration) {
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
         * @return Square the next element in the iteration.
         */
        public Square next() {
            return enumeration.next();
        }
    }
}
