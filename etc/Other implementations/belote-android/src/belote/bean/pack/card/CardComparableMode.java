/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card;

import java.io.Serializable;

import belote.bean.pack.card.rank.Rank;

/**
 * CardComparableMode class.
 * @author Dimitar Karamanov
 */
public abstract class CardComparableMode implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -5914095706786960457L;

    /**
     * CardCompare objects.
     */
    public static final CardComparableMode Standard = new CardComparableModeStandard();

    public static final CardComparableMode NotTrump = new CardComparableModeNotTrump();

    public static final CardComparableMode AllTrump = new CardComparableModeAllTrump();

    /**
     * mode constant used in equals(Object obj)
     */
    private final int mode;

    /**
     * Constructor.
     * @param mode - comparable mode constant.
     */
    protected CardComparableMode(final int mode) {
        this.mode = mode;
    }

    /**
     * Returns hash code.
     * @return hash code.
     */
    public int hashCode() {
        return mode;
    }

    /**
     * The method checks if this CardComparableMode and specified object (CardComparableMode) are equal.
     * @param obj - specified object.
     * @return boolean true if this CardComparableMode is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof CardComparableMode) {
            final CardComparableMode cardCompareMode = (CardComparableMode) obj;
            return mode == cardCompareMode.mode;
        }
        return false;
    }

    /**
     * Returns minimum rank.
     * @return Rank minimum rank.
     */
    public Rank getMinRank() {
        return Rank.Seven;
    }

    /**
     * Returns max rank.
     * @return Rank max rank.
     */
    public abstract Rank getMaxRank();

    /**
     * Returns next rank order.
     * @param rank before.
     * @return Rank next rank.
     */
    public abstract Rank getRankAfter(final Rank rank);

    /**
     * Returns previous rank order.
     * @param rank next.
     * @return Rank previous rank.
     */
    public abstract Rank getRankBefore(final Rank rank);

    /**
     * Returns rank's points.
     * @param rank which points are returned.
     * @return int rank's points.
     */
    public abstract int getPoints(final Rank rank);

    /**
     * Compares rank with the specified rank for order using appropriate rank compare method depending on comparable mode.
     * @param rank which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public abstract int compareRankTo(final Rank rank, final Rank object);

    /**
     * Returns if the rank is major ot not
     * @param rank which is checking for major.
     * @return boolean true if the rank is major false otherwise
     */
    public abstract boolean isMajorRank(final Rank rank);

    /**
     * Compares card with the specified card for order using appropriate card compare method depending on comparable mode.
     * @param card which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this card and the specified card are equal > 0 if this card is bigger than the specified card < 0 if this card is
     *         less than the specified card
     */
    public abstract int compareCardTo(final Card card, final Card object);
}

/**
 * CardComparableModeNormal class. Represents normal card compare mode.
 * @author Dimitar Karamanov
 */
final class CardComparableModeStandard extends CardComparableMode {

    /**
	 * 
	 */
    private static final long serialVersionUID = -1040118466593806321L;
    /**
     * CardCompare's constant.
     */
    private static final int COMPARE_MODE_NO = 0;

    /**
     * Constructor.
     */
    protected CardComparableModeStandard() {
        super(COMPARE_MODE_NO);
    }

    /**
     * Returns max rank.
     * @return Rank max rank.
     */
    public Rank getMaxRank() {
        return Rank.Ace;
    }

    /**
     * Returns next rank order.
     * @param rank instance.
     * @return Rank next rank.
     */
    public Rank getRankAfter(final Rank rank) {
        return Rank.getSTRankAfter(rank);
    }

    /**
     * Returns previous rank order.
     * @param rank instance.
     * @return Rank previous rank.
     */
    public Rank getRankBefore(final Rank rank) {
        return Rank.getSTRankBefore(rank);
    }

    /**
     * Returns rank's points.
     * @param rank instance.
     * @return int rank's points.
     */
    public int getPoints(final Rank rank) {
        return 0;
    }

    /**
     * Compares rank with the specified rank for order using appropriate rank compare method depending on \ comparable mode.
     * @param rank which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareRankTo(final Rank rank, final Rank object) {
        return rank.compareTo(object);
    }

    /**
     * Returns if the rank is major ot not.
     * @param rank which is checking for major.
     * @return boolean true if the rank is major false otherwise.
     */
    public boolean isMajorRank(final Rank rank) {
        return rank.compareTo(Rank.King) > 0;
    }

    /**
     * Compares card with the specified card for order using appropriate card compare method depending on comparable mode.
     * @param card which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this card and the specified card are equal > 0 if this card is bigger than the specified card < 0 if this card is
     *         less than the specified card
     */
    public int compareCardTo(final Card card, final Card object) {
        return card.getStHash() - object.getStHash();
    }
}

/**
 * CardComparableModeNotTrump class. Represents not trump card compare mode.
 * 
 * @author Dimitar Karamanov
 */
final class CardComparableModeNotTrump extends CardComparableMode {

    /**
	 * 
	 */
    private static final long serialVersionUID = 7765669382591454371L;
    /**
     * CardCompare's constant.
     */
    private static final int COMPARE_MODE_NT = 1;

    /**
     * Constructor
     */
    protected CardComparableModeNotTrump() {
        super(COMPARE_MODE_NT);
    }

    /**
     * Returns max rank.
     * @return Rank max rank.
     */
    public Rank getMaxRank() {
        return Rank.Ace;
    }

    /**
     * Returns next rank order.
     * @param rank instance.
     * @return Rank next rank.
     */
    public Rank getRankAfter(final Rank rank) {
        return Rank.getNTRankAfter(rank);
    }

    /**
     * Returns previous rank order.
     * @param rank instance.
     * @return Rank previous rank.
     */
    public Rank getRankBefore(final Rank rank) {
        return Rank.getNTRankBefore(rank);
    }

    /**
     * Returns rank's points.
     * @param rank instance.
     * @return int rank's points.
     */
    public int getPoints(final Rank rank) {
        return rank.getNotTrumpPoints();
    }

    /**
     * Compares rank with the specified rank for order using appropriate rank compare method depending on comparable mode.
     * @param rank which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareRankTo(final Rank rank, final Rank object) {
        return rank.compareToNT(object);
    }

    /**
     * Returns if the rank is major ot not.
     * @param rank which is checking for major.
     * @return boolean true if the rank is major false otherwise.
     */
    public boolean isMajorRank(final Rank rank) {
        return rank.compareToNT(Rank.Queen) > 0;
    }

    /**
     * Compares card with the specified card for order using appropriate card compare method depending on comparable mode.
     * @param card which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this card and the specified card are equal > 0 if this card is bigger than the specified card < 0 if this card is
     *         less than the specified card
     */
    public int compareCardTo(final Card card, final Card object) {
        return card.getNtHash() - object.getNtHash();
    }
}

/**
 * CardComparableModeAllTrump class. Represents all trump card compare mode.
 * @author Dimitar Karamanov
 */
final class CardComparableModeAllTrump extends CardComparableMode {

    /**
	 * 
	 */
    private static final long serialVersionUID = -5148641246538269211L;
    /**
     * CardCompare's constant.
     */
    private static final int COMPARE_MODE_AT = 2;

    /**
     * Constructor.
     */
    protected CardComparableModeAllTrump() {
        super(COMPARE_MODE_AT);
    }

    /**
     * Returns max rank.
     * @return Rank max rank.
     */
    public Rank getMaxRank() {
        return Rank.Jack;
    }

    /**
     * Returns next rank order.
     * @param rank instance.
     * @return Rank next rank.
     */
    public Rank getRankAfter(final Rank rank) {
        return Rank.getATRankAfter(rank);
    }

    /**
     * Returns previous rank order.
     * @param rank instance.
     * @return Rank previous rank.
     */
    public Rank getRankBefore(final Rank rank) {
        return Rank.getATRankBefore(rank);
    }

    /**
     * Returns rank's points.
     * @param rank instance.
     * @return int rank's points.
     */
    public int getPoints(final Rank rank) {
        return rank.getTrumpPoints();
    }

    /**
     * Compares rank with the specified rank for order using appropriate rank compare method depending on comparable mode.
     * @param rank which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this rank and the specified rank are equal > 0 if this rank is bigger than the specified rank < 0 if this rank is
     *         less than the specified rank
     */
    public int compareRankTo(final Rank rank, final Rank object) {
        return rank.compareToAT(object);
    }

    /**
     * Returns if the rank is major ot not.
     * @param rank which is checking for major.
     * @return boolean true if the rank is major false otherwise.
     */
    public boolean isMajorRank(final Rank rank) {
        return rank.compareToAT(Rank.Ten) > 0;
    }

    /**
     * Compares card with the specified card for order using appropriate card compare method depending on comparable mode.
     * @param card which will be compare to other one.
     * @param object the comparable object.
     * @return int value which may be: = 0 if this card and the specified card are equal > 0 if this card is bigger than the specified card < 0 if this card is
     *         less than the specified card
     */
    public int compareCardTo(final Card card, final Card object) {
        return card.getAtHash() - object.getAtHash();
    }
}
