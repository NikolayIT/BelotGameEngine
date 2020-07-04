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

import java.io.Serializable;

import belote.bean.pack.card.rank.Rank;

/**
 * RankComparator base class.
 * @author Dimitar Karamanov
 */
public abstract class RankComparator implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 4256022116248582220L;

    /**
     * All trump comparator.
     */
    public final static RankComparator atComparator = new AllTrumpRankComparator();

    /**
     * Not trump comparator.
     */
    public final static RankComparator ntComparator = new NotTrumpRankComparator();

    /**
     * Standard comparator.
     */
    public final static RankComparator stComparator = new StandardRankComparator();

    /**
     * Compares rank a with b ones.
     * @param a first comparable object.
     * @param b second comparable object.
     * @return int value which may be: = 0 if both specified objects are equal or null > 0 if first object is not null and bigger than the second specified
     *         object or the second is null < 0 if second object is not null and bigger than the first specified object or the first is null
     */
    public abstract int compare(Rank a, Rank b);
}
