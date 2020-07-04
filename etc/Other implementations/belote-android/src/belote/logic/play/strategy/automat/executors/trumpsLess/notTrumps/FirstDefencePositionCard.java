/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps;

import belote.bean.Game;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;
import belote.logic.play.strategy.automat.executors.trumpsLess.NeedlessCard;
import belote.logic.play.strategy.automat.methods.MinimumSuitCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.MaximumSuitLeftCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.notTrump.HookCard;

/**
 * NotTrumpFirstDefencePositionCard executor. Used in NotTrumpPlayStategy getFirstDefencePositionCard().
 * @author Dimitar Karamanov
 */
public final class FirstDefencePositionCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public FirstDefencePositionCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new HookCard(game));
        register(new MaximumSuitLeftCard(game));
        register(new MinimumSuitCard(game));
        register(new NeedlessCard(game));
    }
}
