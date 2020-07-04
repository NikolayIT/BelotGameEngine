/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean;

import java.io.Serializable;

import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;

/**
 * Couple class.
 * @author Dimitar Karamanov
 */
public final class Couple implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -7275183680786350586L;

    /**
     * Array couples count constant.
     */
    public static final int MAX_COUNT = Suit.getSuitCount();

    /**
     * Couple points.
     */
    private static final int COUPLE_POINTS = 20;

    /**
     * Internal container. The maximum couple counts equals to suits count. For a single game is possible only one couple per suit.
     */
    private final boolean coupleSuit[] = new boolean[MAX_COUNT];

    /**
     * Constructor.
     */
    public Couple() {
        super();
    }

    /**
     * Sets couple for provided suit.
     * @param suit provided suit.
     */
    public void setCouple(final Suit suit) {
        coupleSuit[suit.getSuitOrder()] = true;
    }

    /**
     * Returns the sum of all couples points.
     * @return int couples points.
     */
    public int getCouplePoints() {
        int result = 0;
        for (final SuitIterator suitIterator = Suit.iterator(); suitIterator.hasNext();) {
            result += getCouplePoints(suitIterator.next());
        }
        return result;
    }

    /**
     * Returns the count of couples stored in the container.
     * @return int couples count.
     */
    public int getCoupleCount() {
        int result = 0;
        for (final SuitIterator suitIterator = Suit.iterator(); suitIterator.hasNext();) {
            final Suit suit = suitIterator.next();
            if (hasCouple(suit)) {
                result++;
            }
        }
        return result;
    }

    /**
     * Returns true if has couple from provided suit.
     * @param suit provided suit.
     * @return boolean true if has couple false otherwise.
     */
    public boolean hasCouple(final Suit suit) {
        return coupleSuit[suit.getSuitOrder()];
    }

    /**
     * Returns couple points for specified suit.
     * @param suit specified suit.
     * @return int suit couple points.
     */
    private int getCouplePoints(final Suit suit) {
        if (coupleSuit[suit.getSuitOrder()]) {
            return COUPLE_POINTS;
        }
        return 0;
    }

    /**
     * Clears couples points.
     */
    public void clear() {
        for (int suit = 0; suit < coupleSuit.length; suit++) {
            coupleSuit[suit] = false;
        }
    }
}
