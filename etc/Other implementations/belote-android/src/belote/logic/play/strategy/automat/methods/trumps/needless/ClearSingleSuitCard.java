/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.needless;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * ClearSingleSuitCard class. PlayCardMethod which implements the logic of playing an no needed single suit card.
 * @author Dimitar Karmaanov.
 */
public final class ClearSingleSuitCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public ClearSingleSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        Card result = null;
        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();
            final Card card = player.getCards().findMinSuitCard(suit);

            if (card != null && !suit.equals(trump)) {
                final int suitCount = player.getCards().getSuitCount(suit);
                final boolean isSingle = suitCount == 1;
                final boolean isMeter = suitCount + getPassedSuitCardsCount(suit) == Rank.getRankCount();

                final Card max = player.getCards().findMaxSuitCard(suit);
                final boolean powerSuit = max != null && isMaxSuitCardLeft(max, true);

                if (isSingle && !powerSuit && !isMeter && max != null && max.compareRankTo(Rank.King) <= 0) {
                    if (result == null || result.compareRankTo(card) > 0) {
                        result = card;
                    }
                }
            }
        }
        return result;
    }
}