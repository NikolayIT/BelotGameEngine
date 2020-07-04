/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumpsLess;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * MinAboveCard class. PlayCardMethod which implements the logic of playing the minimum card which is above (bigger) from the attack suit card.
 * @author Dimitar Karamanov
 */
public final class MinimumAboveCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MinimumAboveCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        final Card handAttackSuitCard = game.getTrickCards().getHandAttackSuitCard();
        if (handAttackSuitCard != null) {
            return player.getCards().findMinAboveCard(handAttackSuitCard);
        }
        return null;
    }
}