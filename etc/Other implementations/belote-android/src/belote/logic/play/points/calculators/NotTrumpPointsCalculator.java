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
 * NTPointsCalculator class. Points calculator class for No Trump game.
 * @author Dimitar Karamanov
 */
public final class NotTrumpPointsCalculator extends PointsCalculator {

    /**
     * Digits used for rounding.
     */
    private static final int ROUND_DIGID = 6;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public NotTrumpPointsCalculator(Game game) {
        super(game);
    }

    /**
     * Calculates team points.
     * @param team for which the points will be calculated.
     */
    public void calculateTeamPoints(Team team) {
        final PointsInfo gamePointsInfo = team.getPointsInfo();
        gamePointsInfo.resetAllPoints();
        // Calculate cards points
        gamePointsInfo.setCardPoints(team.getHandsPoints());
        // Calculate capot' points
        gamePointsInfo.setCapotPoints(team.getCapotPoints());
        // Calculate last hand
        calculateLastHand(team);
        // Calculate total points
        final int total = (gamePointsInfo.getCardPoints() + gamePointsInfo.getLastHand()) * 2 + gamePointsInfo.getCapotPoints();
        gamePointsInfo.setTotalPoints(total);
    }

    /**
     * Rounds team points.
     * @param team for which the points will be rounded.
     */
    public void trickTeamPoints(Team team) {
        final PointsInfo gamePointsInfo = team.getPointsInfo();
        final int lastDigit = gamePointsInfo.getTotalPoints() % 10;
        final int additionalPoint = lastDigit < ROUND_DIGID ? 0 : 1;
        final int totalRound = gamePointsInfo.getTotalPoints() / 10 + additionalPoint;
        gamePointsInfo.setTotalTrickPoints(totalRound);
    }
}