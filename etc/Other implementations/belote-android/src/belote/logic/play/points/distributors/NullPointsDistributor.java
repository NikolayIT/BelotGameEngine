/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.points.distributors;

import belote.bean.Game;
import belote.bean.announce.Announce;

/**
 * WinGamePointsDistributor distributor class. Points distributor class for win game.
 * @author Dimitar Karamanov
 */
public final class NullPointsDistributor extends PointsDistributor {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public NullPointsDistributor(Game game) {
        super(game);
    }

    /**
     * Distributes points when the game type is TP_NORMAL.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsNormalGame(Announce normalAnnounce) {
        // Do nothing it's NULL object pattern implementation
    }

    /**
     * Distributes points when the game type is TP_DOUBLE.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsDoubleGame(Announce normalAnnounce) {
        // Do nothing it's NULL object pattern implementation
    }

    /**
     * Distributes points when the game type is TP_REDOUBLE.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsRedoubleGame(Announce normalAnnounce) {
        // Do nothing it's NULL object pattern implementation
    }
}