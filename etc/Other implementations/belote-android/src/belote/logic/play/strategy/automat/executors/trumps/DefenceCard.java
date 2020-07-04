/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors.trumps;

import belote.bean.Game;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;

/**
 * DefenceCard executor. Used in TrumpPlayStategy getXXXDefencePositionCard().
 * @author Dimitar Karamanov
 */
public final class DefenceCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public DefenceCard(final Game game) {
        super(game);
        // Register play card methods.
        // When attack card is trump
        register(new DefenceTrumpAttackCard(game));
        // When the player has from attack suit card or have to/can play trump
        register(new DefenceNoTrumpAttackCard(game));
        // When player play no needed card
        register(new NeedlessCard(game));
    }
}
