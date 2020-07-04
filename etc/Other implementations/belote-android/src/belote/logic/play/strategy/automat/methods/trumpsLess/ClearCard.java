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
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitIterator;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * ClearCard class. PlayCardMethod which implements the logic of playing a "clear" card in not color game.
 * @author Dimitar Karamanov
 */
public final class ClearCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public ClearCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;
        for (final SuitIterator si = Suit.iterator(); si.hasNext();) {
            final Suit suit = si.next();
            if (!isPlayerPowerSuit(player, suit)) {
                final Card card = player.getCards().findMinSuitCard(suit);
                if (card != null) {
                    final int suitCount = player.getCards().getSuitCount(suit);
                    final boolean moreTwo = suitCount > TWO_CARDS_COUNT;
                    final boolean moreOne = suitCount > SINGLE_CARD_COUNT;
                    final boolean isMeter = suitCount + getPassedSuitCardsCount(suit) == Rank.getRankCount();

                    final Card max = player.getCards().findMaxSuitCard(suit);
                    final boolean hasMaxSuit = max != null && isMaxSuitCardLeft(max, true);
                    final boolean isMajorCard = max != null && max.isMajorCard();

                    if ((moreTwo || ((hasMaxSuit || !isMajorCard) && moreOne)) && !isMeter) {
                        if (result == null || result.compareRankTo(card) > 0) {
                            result = card;
                        }
                    }
                }
            }
        }
        return result;
    }
}