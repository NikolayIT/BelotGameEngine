/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card.rank.comparator;

import belote.bean.pack.card.rank.Rank;

/**
 * All trump rank comparator.
 * @author Dimitar Karamanov
 */
public final class AllTrumpRankComparator extends RankComparator {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 6611295525138045329L;

    /**
     * Constructor.
     */
    protected AllTrumpRankComparator() {
        super();
    }

    /**
     * Compares rank a with b ones.
     * @param a first comparable object.
     * @param b second comparable object.
     * @return int value which may be: = 0 if both specified objects are equal or null > 0 if first object is not null and bigger than the second specified
     *         object or the second is null < 0 if second object is not null and bigger than the first specified object or the first is null
     */
    public int compare(Rank a, Rank b) {
        return a.compareToAT(b);
    }
}
