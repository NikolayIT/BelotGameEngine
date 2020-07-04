/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card.rank;

import java.util.ArrayList;
import java.util.Iterator;

import belote.base.ComparableObject;
import belote.bean.pack.card.rank.comparator.RankComparator;

/**
 * Rank class Represents card's rank which has one of the following values 7, 8, 9, 10, J, Q, K, A.
 * @author Dimitar Karamanov
 */
public abstract class Rank extends ComparableObject {

    /**
	 * 
	 */
    private static final long serialVersionUID = 8435657626968675818L;

    /**
     * Seven ranks objects.
     */
    public static final Rank Seven = new Seven();

    /**
     * Eight ranks objects.
     */
    public static final Rank Eight = new Eight();

    /**
     * Nine ranks objects.
     */
    public static final Rank Nine = new Nine();

    /**
     * Ten ranks objects.
     */
    public static final Rank Ten = new Ten();

    /**
     * Jack ranks objects.
     */
    public static final Rank Jack = new Jack();

    /**
     * Queen ranks objects.
     */
    public static final Rank Queen = new Queen();

    /**
     * King ranks objects.
     */
    public static final Rank King = new King();

    /**
     * Ace ranks objects.
     */
    public static final Rank Ace = new Ace();

    /**
     * Ranks list (Used for iterating).
     */
    private final static ArrayList<Rank> rankList;

    /**
     * AT rank's constant.
     */
    private final int atRank;

    /**
     * NT rank's constant.
     */
    private final int ntRank;

    /**
     * Default (standard) rank's constant.
     */
    private final int stRank;

    /**
     * Rank's AT points.
     */
    private final int atPoints;

    /**
     * Rank's NT points.
     */
    private final int ntPoints;

    /**
     * Rank's square (4 cards) points.
     */
    private final int sqPoints;

    /**
     * Fills the rank list (static initialization).
     */
    static {
        rankList = new ArrayList<Rank>();

        rankList.add(Seven);
        rankList.add(Eight);
        rankList.add(Nine);
        rankList.add(Jack);
        rankList.add(Queen);
        rankList.add(King);
        rankList.add(Ten);
        rankList.add(Ace);
    }

    /**
     * Constructor.
     * @param stRank default rank value.
     * @param atRank AT rank value.
     * @param ntRank NT rank value.
     * @param shortName short name.
     * @param atPoints AT rank's points.
     * @param ntPoints NT rank's points.
     * @param eqPoints equal cards rank points.
     */
    protected Rank(final int stRank, final int atRank, final int ntRank, final int atPoints, final int ntPoints, final int eqPoints) {
        this.stRank = stRank;
        this.atRank = atRank;
        this.ntRank = ntRank;
        this.atPoints = atPoints;
        this.ntPoints = ntPoints;
        this.sqPoints = eqPoints;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with ofbuscating. Don't use this method to represent the object. When the project is compiled with ofbuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return getClassShortName();
    }

    /**
     * Returns a rank's string first sign representation.
     * @return String a rank's string sign representation.
     */
    public String getRankSign() {
        final String longName = getClassShortName();
        if (longName.length() > 0) {
            return longName.substring(0, 1);
        }
        return "";
    }

    /**
     * Returns hash code.
     * @return hash code.
     */
    public int hashCode() {
        return stRank;
    }

    /**
     * The method checks if this rank and specified object (rank) are equal.
     * @param obj specified object.
     * @return boolean true if this rank is equal to specified object and false otherwise.
     */
    public boolean equals(Object obj) {
        if (obj instanceof Rank) {
            return stRank == ((Rank) obj).stRank;
        }
        return false;
    }

    /**
     * Compares this rank with the specified rank for order using standard rank's value.
     * @param obj specified object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareTo(final Object obj) {
        final Rank compRank = (Rank) obj;
        return stRank - compRank.stRank;
    }

    /**
     * Compares this rank with the specified rank for order using AT rank's value.
     * @param obj specified object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareToAT(final Object obj) {
        final Rank compRank = (Rank) obj;
        return atRank - compRank.atRank;
    }

    /**
     * Compares this rank with the specified rank for order using NT rank's value.
     * @param obj specified object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareToNT(final Object obj) {
        final Rank compRank = (Rank) obj;
        return ntRank - compRank.ntRank;
    }

    /**
     * Returns AT points.
     * @return int rank's trump points.
     */
    public int getTrumpPoints() {
        return atPoints;
    }

    /**
     * Returns NT points.
     * @return int rank's not trump points.
     */
    public int getNotTrumpPoints() {
        return ntPoints;
    }

    /**
     * Returns square points.
     * @return int rank's square points.
     */
    public int getSquarePoints() {
        return sqPoints;
    }

    /**
     * Returns standard rank order.
     * @return int standard rank's order.
     */
    public int getSTRankOrder() {
        return stRank;
    }

    /**
     * Returns NT rank order.
     * @return int not trump rank's order.
     */
    public int getNTRankOrder() {
        return ntRank;
    }

    /**
     * Returns AT rank order.
     * @return int all trump rank's order.
     */
    public int getATRankOrder() {
        return atRank;
    }

    /**
     * Returns rank iterator.
     * @return RankIterator rank iterator.
     */
    public static RankIterator iterator() {
        return new RankIteratorImpl(rankList.iterator());
    }

    /**
     * Returns rank count (8).
     * @return int rank count.
     */
    public static int getRankCount() {
        return rankList.size();
    }

    /**
     * Returns next rank using provided RankComparator.
     * @param rank specified rank.
     * @param rankComparator used for finding rank after.
     * @return Rank next standard rank.
     */
    private static Rank getRankAfter(final Rank rank, final RankComparator rankComparator) {
        for (final RankIterator rankIterator = iterator(); rankIterator.hasNext();) {
            final Rank compRank = rankIterator.next();
            if (rankComparator.compare(compRank, rank) == 1) {
                return compRank;
            }
        }
        return rank;
    }

    /**
     * Returns previous rank using provided RankComparator.
     * @param rank specified rank.
     * @param rankComparator used for finding rank before.
     * @return Rank previous standard rank.
     */
    private static Rank getRankBefore(final Rank rank, final RankComparator rankComparator) {
        for (final RankIterator rankIterator = iterator(); rankIterator.hasNext();) {
            final Rank compRank = rankIterator.next();
            if (rankComparator.compare(compRank, rank) == -1) {
                return compRank;
            }
        }
        return rank;
    }

    /**
     * Returns next rank using standard rank order.
     * @param rank specified rank.
     * @return Rank next standard rank.
     */
    public static Rank getSTRankAfter(final Rank rank) {
        return getRankAfter(rank, RankComparator.stComparator);
    }

    /**
     * Returns next rank using NT rank order.
     * @param rank specified rank.
     * @return Rank next NT rank.
     */
    public static Rank getNTRankAfter(final Rank rank) {
        return getRankAfter(rank, RankComparator.ntComparator);
    }

    /**
     * Returns next rank using AT rank order.
     * @param rank specified rank.
     * @return Rank next AT rank.
     */
    public static Rank getATRankAfter(final Rank rank) {
        return getRankAfter(rank, RankComparator.atComparator);
    }

    /**
     * Returns previous rank using standard rank order.
     * @param rank specified rank.
     * @return Rank previous standard rank.
     */
    public static Rank getSTRankBefore(final Rank rank) {
        return getRankBefore(rank, RankComparator.stComparator);
    }

    /**
     * Returns previous rank using NT rank order.
     * @param rank specified rank.
     * @return Rank previous NT rank.
     */
    public static Rank getNTRankBefore(final Rank rank) {
        return getRankBefore(rank, RankComparator.ntComparator);
    }

    /**
     * Returns previous rank using AT rank order.
     * @param rank specified rank.
     * @return Rank previous AT rank.
     */
    public static Rank getATRankBefore(final Rank rank) {
        return getRankBefore(rank, RankComparator.atComparator);
    }

    /**
     * RankIteratorImpl class Implements RankIterator interface.
     */
    private static class RankIteratorImpl implements RankIterator {

        /**
         * The internal collection enumerator.
         */
        private final Iterator<Rank> enumeration;

        /**
         * Constructor.
         * @param enumeration the internal collection enumerator.
         */
        public RankIteratorImpl(final Iterator<Rank> enumeration) {
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
         * @return Rank the next element in the iteration.
         */
        public Rank next() {
            return enumeration.next();
        }
    }
}
