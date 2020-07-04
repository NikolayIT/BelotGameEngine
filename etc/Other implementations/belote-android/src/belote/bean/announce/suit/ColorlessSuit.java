/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.announce.suit;

/**
 * TrumplessSuit class. The abstract base class of "all trump" and "not trump" announce suits.
 * @author Dimitar Karamanov
 */
public abstract class ColorlessSuit extends AnnounceSuit {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -1853693277831872107L;

    /**
     * Game base points constant.
     */
    private static final int GAME_BASE_POINTS = 26;

    /**
     * Constructor.
     * @param type Type.
     */
    protected ColorlessSuit(final int type) {
        super(type);
    }

    /**
     * Trump suit classes are Club, Diamond, Heart and Spade. If the objects is instance of some of them the result is true otherwise is false. This is the base
     * class for AllTrump and NotTrump announce suits so the method result is always false.
     * @return boolean true if is color suit false otherwise.
     */
    public final boolean isTrumpSuit() {
        return false;
    }

    /**
     * Returns the game base points for that announce suit. The points are used in double and redouble calculation. For AllTrump and NotTrump announce suits the
     * base points are the same.
     * @return int belote game base points.
     */
    public final int getBasePoints() {
        return GAME_BASE_POINTS;
    }
}
