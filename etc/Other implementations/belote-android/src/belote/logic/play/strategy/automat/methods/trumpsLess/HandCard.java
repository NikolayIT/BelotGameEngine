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
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * HandCard class. PlayCardMethod which implements the logic of playing a hand card in a not color game.
 * @author Dimitar Karamanov.
 */
public final class HandCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public HandCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            final Card card = player.getCards().findMaxSuitCard(suit);

            if (card != null && isMaxSuitCardLeft(card, true)) {
                return card;
            }
        }
        return null;
    }
}