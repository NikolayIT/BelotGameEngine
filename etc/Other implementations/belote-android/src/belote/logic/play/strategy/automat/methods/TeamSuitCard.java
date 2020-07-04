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
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * TeamSuitCard class. PlayCardMethod which implements the logic of playing the minimum card from the first found team suit.
 * @author Dimitar Karamanov
 */
public final class TeamSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public TeamSuitCard(Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        final Suit trump = getTrump();
        
        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            if (trump == null || !trump.equals(suit)) {
                if (isTeamSuit(suit, player.getTeam())) {
                    final Card card = player.getCards().findMinSuitCard(suit);
                    if (card != null) {
                        return card;
                    }
                }
            }
        }
        return null;
    }
}