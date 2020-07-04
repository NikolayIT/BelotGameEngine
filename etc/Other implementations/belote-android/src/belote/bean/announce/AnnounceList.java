/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.announce;

import java.io.Serializable;
import java.util.ArrayList;
import java.util.Iterator;

import belote.bean.Player;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.announce.type.AnnounceType;

/**
 * AnnounceList class. Wrapper class of system collection used to hold and access Announce instances.
 * @author Dimitar Karamanov
 */
public final class AnnounceList implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 763763319248786518L;

    /**
     * Constant used to determine end announce tour.
     */
    private final int PASS_COUNT = 3;

    /**
     * Internal container object.
     */
    private final ArrayList<Announce> announces = new ArrayList<Announce>();

    /**
     * Constructor.
     */
    public AnnounceList() {
        super();
    }

    /**
     * Clears the announce list.
     */
    public void clear() {
        announces.clear();
    }

    /**
     * Adds an announce instance to the list.
     * @param announce to be added.
     */
    public void add(final Announce announce) {
        announces.add(announce);
    }

    /**
     * Returns the count of announce instances stored in the list.
     * @return announces count.
     */
    public int getCount() {
        return announces.size();
    }

    /**
     * Returns the announce from the list with provided index. If there is no element with that index an Exception will be thrown.
     * @param index of the announce.
     * @return Announce instance with provided index or Exception is thrown.
     */
    public Announce getAnnounce(final int index) {
        return (Announce) announces.get(index);
    }

    /**
     * Returns the last order announce having not pass suit.
     * @return Announce last not pass (contract) announce.
     */
    public Announce getContractAnnounce() {
        return getContractAnnounce(null, null);
    }

    /**
     * Returns the last normal (no double or redouble) announce.
     * @return Announce last normal (open contract) announce.
     */
    public Announce getOpenContractAnnounce() {
        return getContractAnnounce(AnnounceType.Normal, null);
    }

    /**
     * Returns the last normal (no double or redouble) announce.
     * @return Announce last normal (open contract) announce.
     */
    public Announce getDoubleAnnounce() {
        return getContractAnnounce(AnnounceType.Double, null);
    }

    /**
     * Returns the last normal (no double or redouble) announce.
     * @return Announce last normal (open contract) announce.
     */
    public Announce getRedoubleAnnounce() {
        return getContractAnnounce(AnnounceType.Redouble, null);
    }

    /**
     * Returns last not pass announce for the provided player.
     * @param player player.
     * @return Announce last not pass player' announce.
     */
    public Announce getContractAnnounce(final Player player) {
        return getContractAnnounce(null, player);
    }

    /**
     * Returns last normal (no double or redouble) announce.
     * @param announceType the type of searching announce or null for all types.
     * @param player to filter announces for the provided player or null for all.
     * @return Announce last normal announce.
     */
    private Announce getContractAnnounce(final AnnounceType announceType, final Player player) {
        for (int i = announces.size() - 1; i >= 0; i--) {
            final Announce announce = getAnnounce(i);
            final boolean noPassAnnounce = !announce.getAnnounceSuit().equals(AnnounceSuit.Pass);
            final boolean normalAnnounce = announceType == null || announce.getType().equals(announceType);
            final boolean fitPlayer = player == null || announce.getPlayer().equals(player);
            if (noPassAnnounce && normalAnnounce && fitPlayer) {
                return announce;
            }
        }
        return null;
    }

    /**
     * Returns the player did the last normal announce.
     * @return Player who announced the last normal announce.
     */
    public Player getOpenContractAnnouncePlayer() {
        Announce announce = getOpenContractAnnounce();
        return announce == null ? null : announce.getPlayer();
    }

    /**
     * Returns a list with announces whose suit equals to the provided one.
     * @param suit provided suit.
     * @return AnnounceList a list with announces whose suit equals to the provided one.
     */
    public AnnounceList getSuitAnnounces(AnnounceSuit suit) {
        return getAnnounces(null, suit);
    }

    /**
     * Returns a list with the provided player announces.
     * @param player provided player.
     * @return AnnounceList a list with the provided player announces.
     */
    public AnnounceList getPlayerAnnounces(Player player) {
        return getAnnounces(player, null);
    }

    /**
     * Returns a list with the provided player and suit announces.
     * @param player provided player.
     * @param suit provided suit.
     * @return AnnounceList a list with the provided player announces.
     */
    private AnnounceList getAnnounces(final Player player, final AnnounceSuit suit) {
        AnnounceList result = new AnnounceList();
        for (int i = 0, n = announces.size(); i < n; i++) {
            final Announce announce = this.getAnnounce(i);
            final boolean fitAnnounceSuit = suit == null || announce.getAnnounceSuit().equals(suit);
            final boolean fitPlayer = player == null || announce.getPlayer().equals(player);

            if (fitAnnounceSuit && fitPlayer) {
                result.add(announce);
            }
        }
        return result;
    }

    /**
     * Returns the count of the pass announce which are on the end of the list.
     * @return int the count of pass announces.
     */
    public int getCurrentPassAnnouncesCount() {
        int result = 0;
        for (int i = announces.size() - 1; i >= 0; i--) {
            final Announce announce = getAnnounce(i);
            if (announce.getAnnounceSuit().equals(AnnounceSuit.Pass)) {
                result++;
            } else {
                break;
            }
        }
        return result;
    }

    /**
     * Returns true if another announce is possible to be made, false otherwise. If the last made announce are pass ones and their count is more than 2 is
     * impossible to be made a new announce.
     * @return boolean true if a new announce can be declared, false otherwise.
     */
    public boolean canDeal() {
        // if there is no announce we need 4 passes to end deal
        if (announces.size() <= PASS_COUNT) {
            return true;
        }
        // if there is announce we need 3 passes to end deal
        final int passesCount = getCurrentPassAnnouncesCount();
        return passesCount < PASS_COUNT;
    }

    /**
     * Returns iterator associated with collection.
     * @return AnnounceIterator instance.
     */
    public AnnounceIterator iterator() {
        return new AnnounceIteratorImpl(announces.iterator());
    }

    /**
     * AnnounceIteratorImpl class. Implements AnnounceIterator interface.
     */
    private static class AnnounceIteratorImpl implements AnnounceIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Announce> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public AnnounceIteratorImpl(final Iterator<Announce> enumeration) {
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
         * @return Announce the next element in the iteration.
         */
        public Announce next() {
            return enumeration.next();
        }
    }
}
