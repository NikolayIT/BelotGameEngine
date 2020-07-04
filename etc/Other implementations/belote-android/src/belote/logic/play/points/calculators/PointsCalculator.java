/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.points.calculators;

import belote.bean.Game;
import belote.bean.Team;
import belote.bean.points.PointsInfo;

/**
 * PointsCalculator class. Base class of all game calculators.
 * @author Dimitar Karamanov
 */
public abstract class PointsCalculator {

    /**
     * Last hand points.
     */
    private final static int LAST_HAND_POINTS = 10;

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public PointsCalculator(Game game) {
        this.game = game;
    }

    /**
     * Calculates teams points.
     */
    public final void calculateTeamsPoints() {
        // Calculate each team points
        for (int i = 0; i < game.getTeamsCount(); i++) {
            calculateTeamPoints(game.getTeam(i));
        }

        // Round each team points (For AT and CL needs to be separated for NT
        // doesn't matter)
        for (int i = 0; i < game.getTeamsCount(); i++) {
            trickTeamPoints(game.getTeam(i));
        }
    }

    /**
     * Calculates team points.
     * @param team for which the points will be calculated.
     */
    public abstract void calculateTeamPoints(Team team);

    /**
     * Tricks team points.
     * @param team for which the points will be rounded.
     */
    public abstract void trickTeamPoints(Team team);

    /**
     * Calculates announce points for provided team.
     * @param team provided one
     */
    protected final void calculateAnnouncePoints(Team team) {
        if (team.compareAnnouncesTo(game.getOppositeTeam(team)) > 0) {
            team.getPointsInfo().setAnnouncePoints(team.getAnnouncePoints());
        }
    }

    /**
     * Calculates last hand.
     * @param team for which the last hand points are calculating.
     */
    protected final void calculateLastHand(Team team) {
        if (team.equals(game.getTrickList().getLastTrickWinnerTeam())) {
            team.getPointsInfo().setLastHand(LAST_HAND_POINTS);
        }
    }

    /**
     * Rounds team points.
     * @param team for which the points will be rounded.
     * @param ROUND_DIGID which is used in points rounding.
     */
    protected final void roundTrumpTeamPoints(Team team, final int ROUND_DIGID) {
        final PointsInfo teamPointsInfo = team.getPointsInfo();
        final PointsInfo enemyPointsInfo = game.getOppositeTeam(team).getPointsInfo();

        final int lastDigit = teamPointsInfo.getTotalPoints() % 10;
        int additionalPoint = 0;

        final boolean lastDigidBiggerPoint = lastDigit > ROUND_DIGID;
        final boolean lastDigidEqualsPoint = lastDigit == ROUND_DIGID && teamPointsInfo.getTotalPoints() < enemyPointsInfo.getTotalPoints();

        if (lastDigidBiggerPoint || lastDigidEqualsPoint) {
            additionalPoint = 1;
        }

        final int totalRound = teamPointsInfo.getTotalPoints() / 10 + additionalPoint;
        teamPointsInfo.setTotalTrickPoints(totalRound);
    }

    /**
     * Calculates team points for Trump or AT game.
     * @param team for which the points will be calculated.
     */
    protected final void calculateTrumpTeamPoints(Team team) {
        final PointsInfo gamePointsInfo = team.getPointsInfo();
        gamePointsInfo.resetAllPoints();
        // Calculate cards points
        gamePointsInfo.setCardPoints(team.getHandsPoints());
        // Calculate couples points
        gamePointsInfo.setCouplesPoints(team.getCouples().getCouplePoints());
        // Calculate announce points
        calculateAnnouncePoints(team);
        // Calculate capot' points
        gamePointsInfo.setCapotPoints(team.getCapotPoints());
        // Calculate last hand
        calculateLastHand(team);
        // Calculate total points
        int total = 0;
        total += gamePointsInfo.getCardPoints();
        total += gamePointsInfo.getLastHand();
        total += gamePointsInfo.getCouplesPoints();
        total += gamePointsInfo.getAnnouncePoints();
        total += gamePointsInfo.getCapotPoints();
        gamePointsInfo.setTotalPoints(total);
    }
}
