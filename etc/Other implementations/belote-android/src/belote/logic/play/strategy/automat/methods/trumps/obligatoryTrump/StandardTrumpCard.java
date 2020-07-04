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
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * StandardTrumpCard class. PlayCardMethod which implements the logic of playing a standard trump card.
 * @author Dimitar Karamanov
 */
public final class StandardTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public StandardTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    protected Card getPlayMethodCard(final Player player, final Suit trump) {
        final int count = player.getCards().getSuitCount(trump);
        if (count > TWO_CARDS_COUNT || isPlayerTeamAnnounce(player)) {
            final Card card = player.getCards().findMinSuitCard(trump);
            final Player previousPlayer = game.getPlayerBefore(player);

            if (card != null) {
                if (previousPlayer.equals(game.getAnnounceList().getOpenContractAnnouncePlayer())) {
                    final Card nextCard = player.getCards().findMinAboveCard(card);
                    if (nextCard != null) {
                        return nextCard;
                    }
                }
                return card;
            }
        }
        return null;
    }
}