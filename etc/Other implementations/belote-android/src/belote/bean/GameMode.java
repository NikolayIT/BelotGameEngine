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

/**
 * GameMode class.
 * @author Dimitar Karamanov
 */
public final class GameMode implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -5609712288998797955L;

    /**
     * Game mode announce constant.
     */
    private static final int GM_ANNOUNCE = 0;

    /**
     * Game mode play constant.
     */
    private static final int GM_PLAY = 1;

    /**
     * Game mode info constant.
     */
    private static final int GM_INFO = 2;

    /**
     * Game mode announce type.
     */
    public static final GameMode AnnounceGameMode = new GameMode(GM_ANNOUNCE);

    /**
     * Game mode play type.
     */
    public static final GameMode PlayGameMode = new GameMode(GM_PLAY);

    /**
     * Game mode info type.
     */
    public static final GameMode InfoGameMode = new GameMode(GM_INFO);

    /**
     * Type holder.
     */
    private final int gameMode;

    /**
     * Constructor.
     * @param gameMode game mode constant.
     */
    private GameMode(final int gameMode) {
        this.gameMode = gameMode;
    }

    /**
     * Returns hash code.
     * @return int hash code value.
     */
    public int hashCode() {
        int hash = 7;
        hash = 73 * hash + this.gameMode;
        return hash;
    }

    /**
     * The method checks if this game mode and specified object (GameMode) are equal.
     * @param obj specified object.
     * @return boolean true if this game mode is equal to specified object and false otherwise.
     */
    public boolean equals(Object obj) {
        if (obj instanceof GameMode) {
            return gameMode == ((GameMode) obj).gameMode;
        }
        return false;
    }
}
