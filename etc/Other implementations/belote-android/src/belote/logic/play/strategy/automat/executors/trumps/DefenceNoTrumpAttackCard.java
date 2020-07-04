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
import belote.logic.play.strategy.automat.methods.MinimumSuitCard;
import belote.logic.play.strategy.automat.methods.trumps.noTrumpAttack.EnemyPlayedTrumpCard;
import belote.logic.play.strategy.automat.methods.trumps.noTrumpAttack.MaxSuitLeftCard;
import belote.logic.play.strategy.automat.methods.trumps.noTrumpAttack.HookCard;

/**
 * DefenceNoTrumpAttackCard executor. Implements the obligatory rules for defense player when the attack card is not from trump suit. Used in TrumpDefenceCard
 * executor.
 * @author Dimitar Karamanov
 */
public final class DefenceNoTrumpAttackCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public DefenceNoTrumpAttackCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new EnemyPlayedTrumpCard(game));
        register(new HookCard(game));
        register(new MaxSuitLeftCard(game));
        register(new MinimumSuitCard(game));
        register(new NoTrumpAttackTrumpCardExecutor(game));
    }
}
