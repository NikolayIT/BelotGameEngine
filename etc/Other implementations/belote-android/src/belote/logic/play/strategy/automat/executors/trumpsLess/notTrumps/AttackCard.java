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

/**
 * NotTrumpAttackCard executor. Used in NotTrumpPlayStategy getAttackCard().
 * @author Dimitar Karamanov
 */
public final class AttackCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public AttackCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new AttackCardOnPartnerContract(game));
        register(new AttackCardOnDoubleRedouble(game));
        register(new AttackCardStandard(game));
    }
}
