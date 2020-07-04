/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumpsLess.notTrump;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * NotTrumpSuitHookCard class. PlayCardMethod which implements the logic of playing a hook card (third defence position) in not trump game.
 * @author Dimitar Karamanov
 */
public final class HookCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public HookCard(final Game game) {
        super(game);
    }

    private boolean canHook(final Player player, final Suit suit) {
        if (isFirstDefencePosition()) {
            Player next = game.getPlayerAfter(player);
            Player afterNext = game.getPlayerAfter(next);

            return next.getMissedSuits().contains(suit) && afterNext.getMissedSuits().contains(suit);
        } else if (isSecondDefencePosition()) {
            Player next = game.getPlayerAfter(player);

            return next.getMissedSuits().contains(suit);
        } else if (isThirdDefencePosition()) {
            return true;
        }

        return false;
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        final Card attackCard = game.getTrickCards().getAttackCard();
        final Card handCard = game.getTrickCards().getHandAttackSuitCard();
        final Player handPlayer = game.getPlayerByCard(handCard);

        if (attackCard != null && handCard != null && handPlayer != null && canHook(player, attackCard.getSuit())) {
            final Card maxSuitCard = player.getCards().findMaxSuitCard(attackCard.getSuit());
            if (maxSuitCard != null) {
                // is meter suit and can get the card
                if (isMeterSuitCard(player, maxSuitCard) && maxSuitCard.compareTo(handCard) > 0) {
                    return maxSuitCard;
                }

                final Player partner = player.getPartner();

                if (partner.equals(handPlayer)) {
                    if (!isMaxSuitCardLeft(maxSuitCard, true) || player.getCards().hasPrevFromSameSuit(maxSuitCard)) {
                        if (!handCard.getRank().equals(Rank.getNTRankBefore(maxSuitCard.getRank()))) {
                            return maxSuitCard;
                        }
                    }
                    return player.getCards().findMinSuitCard(attackCard.getSuit());
                } else {
                    if (isMaxSuitCardLeft(maxSuitCard, false)) {
                        Card minAboveHandCard = player.getCards().findMinAboveCard(handCard);
                        if (minAboveHandCard != null) {
                            return minAboveHandCard;
                        }
                    } else {
                        if (maxSuitCard.compareTo(handCard) > 0) {
                            return maxSuitCard;
                        }
                    }
                }
            }
        }
        return null;
    }
}