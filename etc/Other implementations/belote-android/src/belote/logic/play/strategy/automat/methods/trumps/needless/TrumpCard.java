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
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * TrumpCard class. PlayCardMethod which implements the logic of playing a trump card when there is not what other to play.
 * @author Dimitar Karamanov.
 */
public final class TrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public TrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        final int count = player.getCards().getSuitCount(trump);

        if (isThirdDefencePosition() && count == TWO_CARDS_COUNT) {
            final Card card = player.getCards().findMaxSuitCard(trump);
            if (card != null && !isMaxSuitCardLeft(card, true)) {
                return card;
            }
        }
        
        if (count == SINGLE_CARD_COUNT) {
            final Card card = player.getCards().findMaxSuitCard(trump);
            if (card != null && isMaxSuitCardLeft(card, true)) {
                return null;
            }
        }
        
        return player.getCards().findMinSuitCard(trump);
    }
}