/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumpsLess.allTrump;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * AllTrumpSuitHookCard class. PlayCardMethod which implements the logic of playing a card in all trump game in third defense position, which try to "hook" the
 * hand.
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
        if (attackCard != null && canHook(player, attackCard.getSuit())) {
            if (player.getCards().hasSuitCard(attackCard.getSuit())) {
                final Card handCard = game.getTrickCards().getHandAttackSuitCard();
                final Card card = player.getCards().findMaxSuitCard(attackCard.getSuit());

                if (card != null && handCard != null) {
                    if (isMeterSuitCard(player, card) && card.compareTo(handCard) > 0) {
                        return card;
                    }

                    if (isMaxSuitCardLeft(card, true)) {
                        Card maxAboveCard = player.getCards().findMinAboveCard(handCard);
                        if (maxAboveCard != null) {
                            return maxAboveCard;
                        }
                    } else {
                        if (card.compareTo(handCard) > 0) {
                            return card;
                        }
                    }
                }
            }
        }
        return null;
    }
}