/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.sequence;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;

/**
 * SequencesList Class. Wrapper class of system collection used to hold and access Sequence instances.
 * @author Dimitar Karamanov
 */
public final class SequenceList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 6748182747989159923L;
    
    /**
     * Internal container.
     */
    private ArrayList<Sequence> data = new ArrayList<Sequence>();

    /**
     * Adds a sequence to the list.
     * @param sequence specified sequence.
     */
    public void add(final Sequence sequence) {
        data.add(sequence);
    }

    /**
     * Clears the list contents.
     */
    public void clear() {
        data.clear();
    }

    /**
     * Returns true if the list is empty false otherwise.
     * @return boolean true if the list is empty false otherwise.
     */
    public boolean isEmpty() {
        return data.isEmpty();
    }

    /**
     * Returns list specific iterator.
     * @return SequencesIterator iterator.
     */
    public SequenceIterator iterator() {
        return new SequencesIteratorImpl(data.iterator());
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with ofbuscating. Don't use this method to represent the object. When the project is compiled with ofbuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        final StringBuffer sb = new StringBuffer();

        for (final SequenceIterator it = iterator(); it.hasNext();) {
            if (sb.length() != 0) {
                sb.append(" ");
            }
            sb.append(it.next().toAnonymousString());
        }
        return sb.toString();
    }

    /**
     * SequencesIteratorImpl class. Implements SequencesIterator interface.
     */
    private class SequencesIteratorImpl implements SequenceIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Sequence> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public SequencesIteratorImpl(final Iterator<Sequence> enumeration) {
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
         * @return Sequence the next element in the iteration.
         */
        public Sequence next() {
            return enumeration.next();
        }
    }
}
