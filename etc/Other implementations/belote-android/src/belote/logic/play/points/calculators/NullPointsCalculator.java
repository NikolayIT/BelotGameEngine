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
 * NullPointsCalculator class. Null object implementation of PointsCalculator.
 * @author Dimitar Karamanov
 */
public final class NullPointsCalculator extends PointsCalculator {

    /**
     * Constructor.
     * @param game BelotGame instance
     */
    public NullPointsCalculator(Game game) {
        super(game);
    }

    /**
     * Calculates team points.
     * @param team for which the points will be calculated.
     */
    public void calculateTeamPoints(Team team) {
        // Null object
    }

    /**
     * Tricks team points.
     * @param team for which the points will be rounded.
     */
    public void trickTeamPoints(Team team) {
        // Null object
    }
}
