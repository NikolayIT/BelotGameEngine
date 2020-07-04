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
 * Spade class.
 * @author Dimitar Karamanov
 */
public final class Spade extends ColorSuit {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 8862973313519075903L;

    /**
     * Announce suit type unique constant.
     */
    private static final int ID = 4;

    /**
     * Constructor.
     */
    protected Spade() {
        super(ID);
    }
}