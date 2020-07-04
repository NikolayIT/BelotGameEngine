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
 * TrumpSuit class. The abstract base class of all color announce suits.
 * @author Dimitar Karamanov
 */
public abstract class ColorSuit extends AnnounceSuit {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -5476666027564282061L;

    /**
     * Game base points constant.
     */
    private static final int GAME_BASE_POINTS = 16;

    /**
     * Constructor.
     * @param type Type.
     */
    protected ColorSuit(final int type) {
        super(type);
    }

    /**
     * Trump suit classes are Club, Diamond, Heart and Spade. If the objects is instance of some of them the result is true otherwise is false. This is the base
     * class for all color suits so the method result is always true.
     * @return boolean true if is color suit false otherwise.
     */
    public final boolean isTrumpSuit() {
        return true;
    }

    /**
     * Returns the game base points for that announce suit. The points are used in double and redouble calculation. For color suit the base points are the same.
     * @return int belote game base points.
     */
    public final int getBasePoints() {
        return GAME_BASE_POINTS;
    }
}
