/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.trick;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;

import belote.bean.Player;
import belote.bean.Team;

/**
 * TrickList class. Wrapper class of system collection used to hold and access Trick instances.
 * @author Dimitar Karamanov
 */
public final class TrickList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -8020613612899133438L;
    
    /**
     * Internal container object.
     */
    private ArrayList<Trick> list = new ArrayList<Trick>();

    /**
     * Constructor.
     */
    public TrickList() {
        super();
    }

    /**
     * Clears the announce list.
     */
    public void clear() {
        for (final TrickListIterator iterator = iterator(); iterator.hasNext();) {
            final Trick trick = iterator.next();
            trick.getTrickCards().clear();
        }

        list.clear();
    }

    /**
     * Adds a trick to the list.
     * @param trick which to be added.
     */
    public void add(final Trick trick) {
        list.add(trick);
    }

    /**
     * Returns true if the trick list contains no elements false otherwise.
     * @return boolean true if the trick list is empty false otherwise.
     */
    public boolean isEmpty() {
        return list.isEmpty();
    }

    /**
     * Returns team of the player who win the last trick.
     * @return Team which win the last trick.
     */
    public Team getLastTrickWinnerTeam() {
        Team result = null;
        for (final TrickListIterator iterator = iterator(); iterator.hasNext();) {
            final Trick trick = iterator.next();

            if (!iterator.hasNext()) {
                result = trick.getWinnerPlayer().getTeam();
            }
        }
        return result;
    }
    
    public int getAttackCount(final Player player) {
        int result = 0;
        for (final TrickListIterator iterator = iterator(); iterator.hasNext();) {
            final Trick trick = iterator.next();
            if (player.equals(trick.getAttackPlayer())) {
                result++;
            }
        }
        return result;
    }

    /**
     * Returns a trick list iterator.
     * @return TrickListIterator instance.
     */
    public TrickListIterator iterator() {
        return new TrickListIteratorImpl(list.iterator());
    }

    /**
     * TrickListIteratorImpl class. Implements TrickListIterator interface.
     */
    private static class TrickListIteratorImpl implements TrickListIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Trick> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public TrickListIteratorImpl(final Iterator<Trick> enumeration) {
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
         * @return Trick the next element in the iteration.
         */
        public Trick next() {
            return enumeration.next();
        }
    }
}
