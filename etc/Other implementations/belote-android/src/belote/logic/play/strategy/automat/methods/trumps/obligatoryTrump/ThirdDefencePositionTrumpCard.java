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
 * ThirdDefencePositionTrumpCard class. PlayCardMethod which implements the logic of playing a card when the player is in third defence position.
 * @author Dimitar Karamanov
 */
public final class ThirdDefencePositionTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public ThirdDefencePositionTrumpCard(final Game game) {
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
        final Card maxTrumpCard = player.getCards().findMaxSuitCard(trump);
        if (isThirdDefencePosition() && count == TWO_CARDS_COUNT && maxTrumpCard != null) {
            if (!isMaxSuitCardLeft(maxTrumpCard, true)) {
                return maxTrumpCard;
            }
        }
        return null;
    }
}