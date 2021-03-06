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
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * MaxSuitLeftCard class. PlayCardMethod which implements the logic of playing the maximum by rank suit left card.
 * @author Dimitar Karamanov
 */
public final class MaximumSuitLeftCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MaximumSuitLeftCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        final Card attackCard = game.getTrickCards().getAttackCard();
        if (attackCard != null) {
            final Card card = player.getCards().findMaxSuitCard(attackCard.getSuit());
            if (card != null) {
                if (isMaxSuitCardLeft(card, true) && card.compareTo(game.getTrickCards().getHandAttackSuitCard()) > 0) {
                    return card;
                }
            }
        }
        return null;
    }
}