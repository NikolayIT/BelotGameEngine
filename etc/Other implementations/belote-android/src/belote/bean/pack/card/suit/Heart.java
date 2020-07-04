/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.pack.card.suit;

/**
 * Suit heart class.
 * @author Dimitar Karamanov
 */
public final class Heart extends Suit {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -7054021151876037143L;
    
    /**
     * Heart ID constant.
     */
    private static final int ID = 2;

    /**
     * Constructor.
     */
    protected Heart() {
        super(ID);
    }

    /**
     * Returns suit's color.
     * @return String suit's color.
     */
    public String getSuitColor() {
        return "red";
    }
}