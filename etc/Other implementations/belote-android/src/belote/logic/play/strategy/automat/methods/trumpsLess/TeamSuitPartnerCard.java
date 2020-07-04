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
 * TeamSuitPartnerCard class. PlayCardMethod which implements the logic of playing the minimum card from the first found partner team suit.
 * @author Dimitar Karamanov
 */
public final class TeamSuitPartnerCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public TeamSuitPartnerCard(Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player) {
        for (final SuitIterator iterator = Suit.iterator(); iterator.hasNext();) {
            final Suit suit = iterator.next();

            if (isTeamSuit(suit, player.getTeam())) {

                final int passedSuitCount = getPassedSuitCardsCount(suit);
                final int playerSuitCount = player.getCards().getSuitCount(suit);

                // If the suit is team and the count of left cards == partners
                // cards count (has only from this suit)
                if ((Rank.getRankCount() - passedSuitCount - playerSuitCount) == player.getCards().getSize()) {
                    final Card card = player.getCards().findMaxSuitCard(suit);
                    if (card != null) {
                        return card;
                    }
                }
            }
        }
        return null;
    }
}