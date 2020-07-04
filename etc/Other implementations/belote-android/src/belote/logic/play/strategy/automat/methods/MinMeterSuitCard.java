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
 * MinMeterSuitCard class. PlayCardMethod which implements the logic of playing the minimum by rank meter card.
 * @author Dimitar Karamanov
 */
public final class MinMeterSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MinMeterSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        Card result = null;
        final Suit trump = getTrump();
        for (final PackIterator iterator = player.getCards().iterator(); iterator.hasNext();) {
            final Card card = iterator.next();

            if ((trump == null || !card.getSuit().equals(trump)) && isMeterSuitCard(player, card)) {
                if (result == null || result.compareRankTo(card) > 0) {
                    result = card;
                }
            }
        }
        return result;
    }
}