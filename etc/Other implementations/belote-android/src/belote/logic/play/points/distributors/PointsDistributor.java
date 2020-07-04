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
import belote.bean.announce.type.AnnounceType;

/**
 * PointsDistributor base distributor class. Points calculator class for All Trump game.
 * @author Dimitar Karamanov
 */
public abstract class PointsDistributor {

    /**
     * Belot game internal object.
     */
    protected final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public PointsDistributor(Game game) {
        this.game = game;
    }

    /**
     * Distributes points to the teams.
     */
    public final void distributeTeamsPoints() {
        final Announce normalAnnounce = game.getAnnounceList().getOpenContractAnnounce();
        final Announce lastAnnounce = game.getAnnounceList().getContractAnnounce();

        if (normalAnnounce != null && lastAnnounce != null) {
            if (lastAnnounce.getType().equals(AnnounceType.Normal)) {
                distributeTeamsPointsNormalGame(normalAnnounce);
            } else if (lastAnnounce.getType().equals(AnnounceType.Double)) {
                distributeTeamsPointsDoubleGame(normalAnnounce);
            } else if (lastAnnounce.getType().equals(AnnounceType.Redouble)) {
                distributeTeamsPointsRedoubleGame(normalAnnounce);
            }
        }
    }

    /**
     * Distributes points when the game type is TP_NORMAL.
     * @param normalAnnounce announce.
     */
    public abstract void distributeTeamsPointsNormalGame(Announce normalAnnounce);

    /**
     * Distributes points when the game type is TP_DOUBLE.
     * @param normalAnnounce announce.
     */
    public abstract void distributeTeamsPointsDoubleGame(Announce normalAnnounce);

    /**
     * Distributes points when the game type is TP_REDOUBLE.
     * @param normalAnnounce announce.
     */
    public abstract void distributeTeamsPointsRedoubleGame(Announce normalAnnounce);
}
