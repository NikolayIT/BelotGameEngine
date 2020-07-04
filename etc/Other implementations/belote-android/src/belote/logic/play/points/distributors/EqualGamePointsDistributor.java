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
import belote.bean.Team;
import belote.bean.announce.Announce;

/**
 * WinGamePointsDistributor distributor class. Points distributor class for equal game.
 * @author Dimitar Karamanov
 */
public final class EqualGamePointsDistributor extends PointsDistributor {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public EqualGamePointsDistributor(Game game) {
        super(game);
    }

    /**
     * Distributes points when the game type is TP_NORMAL.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsNormalGame(Announce normalAnnounce) {
        final Team announceTeam = normalAnnounce.getPlayer().getTeam();
        final Team oppositeTeam = game.getOppositeTeam(announceTeam);

        final int announceTeamPoints = announceTeam.getPointsInfo().getTotalTrickPoints();
        final int oppositeTeamPoints = oppositeTeam.getPointsInfo().getTotalTrickPoints();
        final int bothPoints = announceTeamPoints + oppositeTeamPoints;

        final int lowHalfPoints = bothPoints / 2;
        final int hangedPoints = bothPoints - lowHalfPoints;
        game.increaseHangedPoints(hangedPoints);
        announceTeam.getPoints().add(0);
        oppositeTeam.getPoints().add(lowHalfPoints);
    }

    /**
     * Distributes points when the game type is TP_DOUBLE.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsDoubleGame(Announce normalAnnounce) {
        distributeTeamsPointsRedoubleGame(normalAnnounce, 2);
    }

    /**
     * Distributes points when the game type is TP_REDOUBLE.
     * @param normalAnnounce announce.
     */
    public void distributeTeamsPointsRedoubleGame(Announce normalAnnounce) {
        distributeTeamsPointsRedoubleGame(normalAnnounce, 4);
    }

    /**
     * Distributes points when the game type is TP_REDOUBLE.
     * @param normalAnnounce Announce instance.
     * @param multiplicator used to multiply the points.
     */
    private void distributeTeamsPointsRedoubleGame(Announce normalAnnounce, int multiplicator) {
        final Team announceTeam = normalAnnounce.getPlayer().getTeam();
        final Team oppositeTeam = game.getOppositeTeam(announceTeam);

        final int announceTeamPoints = announceTeam.getPointsInfo().getTotalTrickPoints();
        final int oppositeTeamPoints = oppositeTeam.getPointsInfo().getTotalTrickPoints();
        final int bothPoints = announceTeamPoints + oppositeTeamPoints;

        final int hangedPoints = multiplicator * bothPoints;
        game.increaseHangedPoints(hangedPoints);
        announceTeam.getPoints().add(0);
        oppositeTeam.getPoints().add(0);
    }
}
