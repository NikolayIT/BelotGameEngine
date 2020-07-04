/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.points;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;

/**
 * PointList class. Wrapper class of system collection used to hold and access points instances.
 * @author Dimitar Karamanov
 */
public final class PointsList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 8379973340881747881L;
    
    /**
     * Internal container object.
     */
    private final ArrayList<Integer> pointsList = new ArrayList<Integer>();

    /**
     * Constructor.
     */
    public PointsList() {
        super();
    }

    /**
     * Adds points to the list.
     * @param points new points.
     */
    public void add(final int points) {
        pointsList.add(points);
    }

    public int getPointsAt(int index) {
        return pointsList.get(index).intValue();
    }

    /**
     * Returns all points sum.
     * @return int all points sum.
     */
    public int getAllPoints() {
        int result = 0;
        for (final PointsIterator iterator = iterator(); iterator.hasNext();) {
            result += iterator.next();
        }
        return result;
    }

    /**
     * Clears the points list.
     */
    public void clear() {
        pointsList.clear();
    }

    /**
     * Returns elements count.
     * @return int elements count.
     */
    public int size() {
        return pointsList.size();
    }

    /**
     * Returns points list iterator.
     * @return PointsIterator points list iterator.
     */
    public PointsIterator iterator() {
        return new PointsIteratorImpl(pointsList.iterator());
    }

    /**
     * PointsIteratorImpl class. Implements PointsIterator interface.
     */
    private class PointsIteratorImpl implements PointsIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Integer> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public PointsIteratorImpl(final Iterator<Integer> enumeration) {
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
         * @return int the next element in the iteration.
         */
        public int next() {
            final Integer integer = enumeration.next();
            return integer.intValue();
        }
    }
}
