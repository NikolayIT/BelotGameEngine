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
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * DominantSuitCard class. PlayCardMethod which implements the logic of playing a card from the dominant suit.
 * @author Dimitar Karamanov
 */
public final class DominantSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public DominantSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;

        final Suit dominatSuit = player.getCards().getDominantSuit();
        if (dominatSuit != null) {
            final int count = player.getCards().getSuitCount(dominatSuit);
            if (count >= MINIMUM_SUIT_COUNT) {
                final Card max = player.getCards().findMaxSuitCard(dominatSuit);
                if (max != null) {
                    if (isMaxSuitCardLeft(max, true)) {
                        if (count >= FIT_SUIT_COUNT) {
                            result = max;
                        }
                    } else {
                        final Player partner = player.getPartner();
                        if (isEnemyTeamAnnounce(player)
                                || (!partner.getUnwantedSuits().contains(dominatSuit) && !partner.getMissedSuits().contains(dominatSuit))) {
                            if (player.getCards().hasCouple(dominatSuit)) {
                                result = player.getCards().findCard(Rank.Queen, dominatSuit);
                            } else {
                                result = player.getCards().findMinSuitCard(dominatSuit);
                            }
                        }
                    }
                }
            }
        }

        if (result != null) {
            if (isTeamSuitAnnounce(game.getOppositeTeam(player), result.getSuit())) {
                result = null;
            }
        }

        return result;
    }
}
