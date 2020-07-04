/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * MaxSingleNoHandCard class. PlayCardMethod which implements the logic of playing the maximum rank single no hand card (to give it to the partner hand).
 * @author Dimitar Karamanov
 */
public final class MaxSingleNoHandCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MaxSingleNoHandCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        Card result = null;
        final Suit trump = getTrump();
        for (final PackIterator iterator = player.getCards().iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (trump == null || !trump.equals(card.getSuit())) {
                final int suitCount = player.getCards().getSuitCount(card.getSuit());
                if (suitCount == SINGLE_CARD_COUNT && !isMaxSuitCardLeft(card, true)) {
                    if (result == null || result.compareRankTo(card) < 0) {
                        result = card;
                    }
                }
            }
        }
        return result;
    }
}