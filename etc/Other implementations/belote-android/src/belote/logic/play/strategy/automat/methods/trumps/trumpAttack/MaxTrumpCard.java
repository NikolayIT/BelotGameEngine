/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumps.trumpAttack;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.method.BaseTrumpMethod;

/**
 * MaxTrumpCard class. PlayCardMethod which implements the logic of playing the maximum trump card on color game trump attack.
 * @author Dimitar Karamanov
 */
public final class MaxTrumpCard extends BaseTrumpMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MaxTrumpCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @param trump suit.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player, final Suit trump) {
        final Card attackCard = game.getTrickCards().getAttackCard();
        if (attackCard != null && attackCard.getSuit().equals(trump) && isPlayerTeamAnnounce(player)) {
            final Card max = player.getCards().findMaxSuitCard(trump);
            if (max != null && isMaxSuitCardLeft(max, false)) {
                return max;
            }
        }
        return null;
    }
}