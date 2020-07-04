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
 * MeterSuitCard class. PlayCardMethod which implements the logic of playing a meter suit card. When is a color game mode the trump suit is skipped.
 * @author Dimitar Karamanov
 */
public final class MeterSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MeterSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        final Suit trump = getTrump();
        for (final PackIterator iterator = player.getCards().iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if ((trump == null || !card.getSuit().equals(trump)) && isMeterSuitCard(player, card)) {
                return player.getCards().findMaxSuitCard(card.getSuit());
            }
        }
        return null;
    }
}