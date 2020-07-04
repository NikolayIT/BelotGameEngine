/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps.AttackCard;
import belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps.FirstDefencePositionCard;
import belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps.SecondDefencePositionCard;
import belote.logic.play.strategy.automat.executors.trumpsLess.notTrumps.ThirdDefencePositionCard;
import belote.logic.play.strategy.validators.NotTrumpCardValidator;

/**
 * NTPlayCardStrategy class. Not trump strategy playing class.
 * @author Dimitar Karamanov
 */
public final class NotTrumpPlayStrategy extends BasePlayStrategy {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public NotTrumpPlayStrategy(final Game game) {
        super(game, new NotTrumpCardValidator(game), new AttackCard(game), new FirstDefencePositionCard(game),
                new SecondDefencePositionCard(game), new ThirdDefencePositionCard(game));
    }

    /**
     * Returns next attack player.
     * @param attack the round attack card.
     * @return Player next attack player.
     */
    protected final Player getNextAttackPlayer(final Card attack) {
        final Card maxSuit = game.getTrickCards().findMaxSuitCard(attack.getSuit());
        return game.getPlayerByCard(maxSuit);
    }
}
