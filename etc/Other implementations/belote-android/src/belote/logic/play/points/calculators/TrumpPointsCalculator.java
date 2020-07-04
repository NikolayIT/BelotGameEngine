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

/**
 * TrumpPointsCalculator class. Points calculator class for Trump game.
 * @author Dimitar Karamanov
 */
public final class TrumpPointsCalculator extends PointsCalculator {

    /**
     * Digits used for rounding.
     */
    private static final int ROUND_DIGID = 6;

    /**
     * Constructor.
     * @param game belote game instance.
     */
    public TrumpPointsCalculator(Game game) {
        super(game);
    }

    /**
     * Calculates team points.
     * @param team for which the points will be calculated.
     */
    public void calculateTeamPoints(Team team) {
        calculateTrumpTeamPoints(team);
    }

    /**
     * Rounds team points.
     * @param team for which the points will be rounded.
     */
    public void trickTeamPoints(Team team) {
        roundTrumpTeamPoints(team, ROUND_DIGID);
    }
}