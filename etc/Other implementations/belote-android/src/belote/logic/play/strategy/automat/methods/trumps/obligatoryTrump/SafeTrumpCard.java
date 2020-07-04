/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.obligatoryTrump;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.Pack;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * SafeTrumpCard class. PlayCardMethod which implements the logic of playing a safe trump card in color game.
 * @author Dimitar Karamanov.
 */
public final class SafeTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public SafeTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        if (trump != null) {
            final Pack playerCards = player.getCards().getSuitPack(trump);
            final Pack restCards = getOtherPlayersSuitCards(player, trump);

            for (int i = playerCards.getSize() - 1; i >= 0; i--) {
                final Card playerMaxCard = playerCards.findMaxSuitCard(trump); // It
                                                                               // can't
                                                                               // be
                                                                               // null
                final Card restMaxCard = restCards.findMaxSuitCard(trump);
                final Card restMinCard = restCards.findMinSuitCard(trump);

                if (restMaxCard == null || restMinCard == null) {
                    return playerMaxCard;
                }

                if (playerMaxCard.compareTo(restMaxCard) < 0) {
                    return null;
                }

                playerCards.remove(playerMaxCard);
                restCards.remove(restMinCard);
            }
        }
        return null;
    }
}